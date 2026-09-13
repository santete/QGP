using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

namespace Qgp.Api.Auth;

/// <summary>
/// Cấu hình authentication (JWT Bearer) + authorization (RBAC §10.1).
/// Mode=Dev → HS256 mock OIDC (khoá từ env QGP_AUTH_SIGNING_KEY, thiếu thì sinh ephemeral).
/// Mode=Oidc → validate token qua OIDC authority thật (RS256/JWKS).
/// </summary>
public static class AuthSetup
{
    public static IServiceCollection AddQgpAuth(this IServiceCollection services, IConfiguration config)
    {
        var mode = config["Auth:Mode"] ?? "Dev";
        var issuer = config["Auth:Issuer"] ?? "qgp-dev";
        var audience = config["Auth:Audience"] ?? "qgp-api";

        var auth = services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme);

        if (string.Equals(mode, "Oidc", StringComparison.OrdinalIgnoreCase))
        {
            var authority = config["Auth:OidcAuthority"]
                ?? throw new InvalidOperationException("Auth:Mode=Oidc cần Auth:OidcAuthority");
            // Keycloak local chạy http → cho phép tắt HTTPS-metadata (mặc định true = an toàn, prod dùng https).
            var requireHttps = config.GetValue("Auth:RequireHttpsMetadata", true);
            auth.AddJwtBearer(o =>
            {
                o.Authority = authority;
                o.Audience = audience;
                o.MapInboundClaims = false;
                o.RequireHttpsMetadata = requireHttps;
                // iss của token Keycloak = URL realm (== authority), KHÔNG phải "qgp-dev".
                o.TokenValidationParameters = BaseParams(authority, audience);
            });

            // Keycloak trả realm role trong claim realm_access.roles (JSON), KHÔNG phải claim "role" phẳng.
            // ClaimsTransformation flatten + map sang app role (READER..ADMIN) qua Auth:RoleMap → RBAC §10.1 chạy như cũ.
            var roleMap = config.GetSection("Auth:RoleMap").Get<Dictionary<string, string>>()
                ?? new Dictionary<string, string>();
            services.AddSingleton<IClaimsTransformation>(new KeycloakClaimsTransformation(roleMap));
        }
        else
        {
            // Dev: khoá đối xứng từ env (>=32 bytes) hoặc ephemeral random (không commit secret nào).
            var key = ResolveDevKey(config);
            var signing = new DevSigningKey(key, issuer, audience);
            services.AddSingleton(signing);
            services.AddSingleton<DevTokenIssuer>();

            auth.AddJwtBearer(o =>
            {
                o.MapInboundClaims = false;
                var p = BaseParams(issuer, audience);
                p.ValidateIssuerSigningKey = true;
                p.IssuerSigningKey = key;
                o.TokenValidationParameters = p;
            });
        }

        services.AddAuthorizationBuilder().AddQgpPolicies();
        return services;
    }

    private static TokenValidationParameters BaseParams(string issuer, string audience) => new()
    {
        ValidateIssuer = true,
        ValidIssuer = issuer,
        ValidateAudience = true,
        ValidAudience = audience,
        ValidateLifetime = true,
        RoleClaimType = "role",
        NameClaimType = "sub",
        ClockSkew = TimeSpan.FromSeconds(30),
    };

    private static SymmetricSecurityKey ResolveDevKey(IConfiguration config)
    {
        var fromEnv = Environment.GetEnvironmentVariable("QGP_AUTH_SIGNING_KEY")
            ?? config["Auth:DevSigningKey"];

        if (!string.IsNullOrEmpty(fromEnv))
        {
            var bytes = Encoding.UTF8.GetBytes(fromEnv);
            if (bytes.Length < 32)
                throw new InvalidOperationException("QGP_AUTH_SIGNING_KEY phải >= 32 ký tự (HS256).");
            return new SymmetricSecurityKey(bytes);
        }

        // Ephemeral: token chỉ hợp lệ trong vòng đời process (đủ cho dev/CI test).
        return new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(32));
    }

    private static AuthorizationBuilder AddQgpPolicies(this AuthorizationBuilder b)
    {
        foreach (var (policy, roles) in QgpPolicies.RolesFor)
            b.AddPolicy(policy, p => p.RequireRole(roles)); // default deny; cần role trong tập
        return b;
    }
}
