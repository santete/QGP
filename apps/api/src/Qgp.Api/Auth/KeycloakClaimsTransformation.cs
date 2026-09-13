using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;

namespace Qgp.Api.Auth;

/// <summary>
/// Chuyển đổi claim của token Keycloak (chế độ Auth:Mode=Oidc) sang RBAC nội bộ:
/// đọc claim <c>realm_access</c> (JSON <c>{"roles":[...]}</c>), map mỗi realm role của
/// Keycloak sang app role (READER..ADMIN) qua <c>Auth:RoleMap</c>, rồi thêm claim
/// loại <c>"role"</c> (khớp <see cref="Microsoft.IdentityModel.Tokens.TokenValidationParameters.RoleClaimType"/> = "role").
///
/// Default-deny: realm role không có trong RoleMap bị bỏ qua. Idempotent (không nhân đôi
/// claim khi bị gọi nhiều lần). Không đọc secret, không log token (SECURITY_RULES).
/// </summary>
public sealed class KeycloakClaimsTransformation : IClaimsTransformation
{
    private const string RealmAccessClaim = "realm_access";
    private const string RoleClaimType = "role";

    private readonly IReadOnlyDictionary<string, string> _roleMap;

    public KeycloakClaimsTransformation(IReadOnlyDictionary<string, string> roleMap)
    {
        // Keycloak realm role name so khớp case-insensitive (idempotent với cấu hình).
        _roleMap = new Dictionary<string, string>(roleMap, StringComparer.OrdinalIgnoreCase);
    }

    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not ClaimsIdentity identity || !identity.IsAuthenticated)
            return Task.FromResult(principal);

        var realmAccess = identity.FindFirst(RealmAccessClaim)?.Value;
        if (string.IsNullOrWhiteSpace(realmAccess))
            return Task.FromResult(principal);

        foreach (var keycloakRole in ExtractRealmRoles(realmAccess))
        {
            if (_roleMap.TryGetValue(keycloakRole, out var appRole)
                && !identity.HasClaim(RoleClaimType, appRole))
            {
                identity.AddClaim(new Claim(RoleClaimType, appRole));
            }
        }

        return Task.FromResult(principal);
    }

    /// <summary>Parse <c>realm_access.roles</c>; token dị dạng → trả rỗng (không throw).</summary>
    private static IEnumerable<string> ExtractRealmRoles(string realmAccessJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(realmAccessJson);
            if (!doc.RootElement.TryGetProperty("roles", out var rolesEl)
                || rolesEl.ValueKind != JsonValueKind.Array)
                return [];

            // Materialize trước khi doc bị dispose.
            return rolesEl.EnumerateArray()
                .Where(r => r.ValueKind == JsonValueKind.String)
                .Select(r => r.GetString()!)
                .ToArray();
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
