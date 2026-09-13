using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Qgp.Api.Tests;

/// <summary>
/// A7 — security headers + rate limiting (SECURITY_RULES). Dùng endpoint '/' (anonymous, không chạm DB).
/// </summary>
public class SecurityHeadersTests(QgpApiFactory factory) : IClassFixture<QgpApiFactory>
{
    private readonly QgpApiFactory _factory = factory;

    [Fact]
    public async Task Root_has_baseline_security_headers()
    {
        var res = await _factory.CreateClient().GetAsync("/");

        Assert.Equal("nosniff", res.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", res.Headers.GetValues("X-Frame-Options").Single());
        Assert.Equal("no-referrer", res.Headers.GetValues("Referrer-Policy").Single());
    }
}

/// <summary>Rate limit: đặt ngưỡng thấp (2/100s) qua env → request thứ 3 bị 429.</summary>
public class RateLimitTests
{
    private sealed class LowLimitFactory : WebApplicationFactory<Program>
    {
        public LowLimitFactory()
        {
            Environment.SetEnvironmentVariable("RateLimit__Enabled", "true"); // Testing mặc định off → opt-in
            Environment.SetEnvironmentVariable("RateLimit__PermitLimit", "2");
            Environment.SetEnvironmentVariable("RateLimit__WindowSeconds", "100"); // dài để không reset giữa test
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder) => builder.UseEnvironment("Testing");

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                Environment.SetEnvironmentVariable("RateLimit__Enabled", null);
                Environment.SetEnvironmentVariable("RateLimit__PermitLimit", null);
                Environment.SetEnvironmentVariable("RateLimit__WindowSeconds", null);
            }
        }
    }

    [Fact]
    public async Task Exceeding_permit_limit_returns_429()
    {
        using var factory = new LowLimitFactory();
        var client = factory.CreateClient();

        var r1 = await client.GetAsync("/");
        var r2 = await client.GetAsync("/");
        var r3 = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, r1.StatusCode);
        Assert.Equal(HttpStatusCode.OK, r2.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, r3.StatusCode);
    }
}

/// <summary>
/// A5 — secret dạng file (Vault Agent / Docker secrets) override config. Trỏ QGP_SECRETS_DIR vào
/// thư mục tạm chứa file 'Cors__AllowedOrigins' → BE dùng giá trị đó (kiểm qua header CORS).
/// </summary>
public class SecretsFileConfigTests
{
    private sealed class SecretsFactory : WebApplicationFactory<Program>
    {
        public readonly string Dir = Path.Combine(Path.GetTempPath(), "qgp_secrets_" + Guid.NewGuid().ToString("N"));

        public SecretsFactory()
        {
            Directory.CreateDirectory(Dir);
            File.WriteAllText(Path.Combine(Dir, "Cors__AllowedOrigins"), "https://filesecret.test");
            Environment.SetEnvironmentVariable("QGP_SECRETS_DIR", Dir);
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder) => builder.UseEnvironment("Testing");

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                Environment.SetEnvironmentVariable("QGP_SECRETS_DIR", null);
                try { Directory.Delete(Dir, recursive: true); } catch { /* best-effort */ }
            }
        }
    }

    [Fact]
    public async Task File_secret_overrides_cors_origin()
    {
        using var factory = new SecretsFactory();
        var client = factory.CreateClient();
        var req = new HttpRequestMessage(HttpMethod.Get, "/");
        req.Headers.Add("Origin", "https://filesecret.test");

        var res = await client.SendAsync(req);

        Assert.True(res.Headers.Contains("Access-Control-Allow-Origin"));
        Assert.Equal("https://filesecret.test", res.Headers.GetValues("Access-Control-Allow-Origin").Single());
    }
}
