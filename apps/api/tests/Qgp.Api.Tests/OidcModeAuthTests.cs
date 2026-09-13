using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Qgp.Api.Tests;

/// <summary>
/// A1a — chế độ Auth:Mode=Oidc (Keycloak thật). Xác nhận:
/// (1) /auth/dev-login BỊ VÔ HIỆU (gate: DevTokenIssuer chỉ đăng ký ở Mode=Dev) → 404.
/// (2) KeycloakClaimsTransformation được đăng ký vào DI (flatten realm_access.roles).
/// KHÔNG cần Keycloak chạy: dev-login là endpoint anonymous nên không chạm JWKS.
/// </summary>
public class OidcModeAuthTests
{
    // Program.cs đọc config["Auth:Mode"] lúc ĐĂNG KÝ service (trước builder.Build()), nên
    // ConfigureAppConfiguration (áp lúc Build) là quá muộn. Env var được nạp ngay ở CreateBuilder
    // → set env var trong ctor, dọn ở Dispose. Suite chạy tuần tự (xunit.runner.json) nên không leak.
    private sealed class OidcFactory : WebApplicationFactory<Program>
    {
        public OidcFactory()
        {
            Environment.SetEnvironmentVariable("Auth__Mode", "Oidc");
            Environment.SetEnvironmentVariable("Auth__OidcAuthority", "http://localhost:9/realms/qgp");
            Environment.SetEnvironmentVariable("Auth__RequireHttpsMetadata", "false");
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
            => builder.UseEnvironment("Testing");

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                Environment.SetEnvironmentVariable("Auth__Mode", null);
                Environment.SetEnvironmentVariable("Auth__OidcAuthority", null);
                Environment.SetEnvironmentVariable("Auth__RequireHttpsMetadata", null);
            }
        }
    }

    [Fact]
    public async Task DevLogin_disabled_in_oidc_mode_returns_404()
    {
        using var factory = new OidcFactory();
        var client = factory.CreateClient();

        var res = await client.PostAsJsonAsync("/auth/dev-login", new { sub = "x", roles = new[] { "READER" } });

        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public void ClaimsTransformation_registered_in_oidc_mode()
    {
        using var factory = new OidcFactory();
        using var scope = factory.Services.CreateScope();

        var transform = scope.ServiceProvider
            .GetService<Microsoft.AspNetCore.Authentication.IClaimsTransformation>();

        Assert.IsType<Qgp.Api.Auth.KeycloakClaimsTransformation>(transform);
    }
}
