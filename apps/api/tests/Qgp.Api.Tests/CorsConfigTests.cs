using System.Net.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Qgp.Api.Tests;

/// <summary>
/// A4 — CORS origin đọc từ config (bỏ hardcode localhost:5173). Prod set qua env
/// Cors__AllowedOrigins=https://qgp.example.com. Header ACAO chỉ trả cho origin có trong danh sách.
/// </summary>
public class CorsConfigTests
{
    private sealed class CorsFactory : WebApplicationFactory<Program>
    {
        public CorsFactory()
        {
            Environment.SetEnvironmentVariable("Cors__AllowedOrigins", "https://cors.test,https://two.test");
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder) => builder.UseEnvironment("Testing");

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing) Environment.SetEnvironmentVariable("Cors__AllowedOrigins", null);
        }
    }

    [Fact]
    public async Task Allows_configured_origin()
    {
        using var factory = new CorsFactory();
        var client = factory.CreateClient();
        var req = new HttpRequestMessage(HttpMethod.Get, "/healthz");
        req.Headers.Add("Origin", "https://cors.test");

        var res = await client.SendAsync(req);

        Assert.True(res.Headers.Contains("Access-Control-Allow-Origin"));
        Assert.Equal("https://cors.test", res.Headers.GetValues("Access-Control-Allow-Origin").Single());
    }

    [Fact]
    public async Task Does_not_allow_unlisted_origin()
    {
        using var factory = new CorsFactory();
        var client = factory.CreateClient();
        var req = new HttpRequestMessage(HttpMethod.Get, "/healthz");
        req.Headers.Add("Origin", "https://evil.test");

        var res = await client.SendAsync(req);

        Assert.False(res.Headers.Contains("Access-Control-Allow-Origin"));
    }
}
