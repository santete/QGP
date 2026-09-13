using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace Qgp.Api.Auth;

/// <summary>Khoá ký + issuer/audience cho chế độ Dev (mock OIDC). Singleton chia sẻ giữa issuer và validation.</summary>
public sealed record DevSigningKey(SymmetricSecurityKey Key, string Issuer, string Audience);

/// <summary>
/// Phát hành JWT dev (mock OIDC, Q1) — KÝ HS256 bằng khoá dev. Chỉ dùng ở môi trường Dev;
/// prod dùng OIDC provider thật (RS256/JWKS), KHÔNG có service này.
/// </summary>
public sealed class DevTokenIssuer(DevSigningKey signing)
{
    public const int ExpiresSeconds = 900; // 15 phút (SECURITY_RULES: access token ngắn)

    public string Issue(string subject, IEnumerable<string> roles)
    {
        var claims = new List<Claim> { new("sub", subject), new("name", subject) };
        claims.AddRange(roles.Select(r => new Claim("role", r)));

        var creds = new SigningCredentials(signing.Key, SecurityAlgorithms.HmacSha256);
        var now = DateTime.UtcNow;
        var token = new JwtSecurityToken(
            issuer: signing.Issuer,
            audience: signing.Audience,
            claims: claims,
            notBefore: now,
            expires: now.AddSeconds(ExpiresSeconds),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
