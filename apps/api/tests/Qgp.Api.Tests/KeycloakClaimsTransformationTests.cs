using System.Security.Claims;
using Qgp.Api.Auth;

namespace Qgp.Api.Tests;

/// <summary>
/// Unit test cho KeycloakClaimsTransformation (A1a) — KHÔNG cần Keycloak thật.
/// Flatten realm_access.roles (JSON claim của Keycloak) → app role qua Auth:RoleMap.
/// Default-deny: role KC không có trong map bị bỏ qua.
/// </summary>
public class KeycloakClaimsTransformationTests
{
    private static readonly Dictionary<string, string> RoleMap = new()
    {
        ["reader"] = QgpRoles.Reader,
        ["contributor"] = QgpRoles.Contributor,
        ["author"] = QgpRoles.Author,
        ["approver"] = QgpRoles.Approver,
        ["qa_lead"] = QgpRoles.QaLead,
        ["admin"] = QgpRoles.Admin,
    };

    private static ClaimsPrincipal PrincipalWith(params Claim[] claims)
        => new(new ClaimsIdentity(claims, authenticationType: "TestAuth"));

    private static string RealmAccess(params string[] roles)
        => "{\"roles\":[" + string.Join(",", roles.Select(r => $"\"{r}\"")) + "]}";

    private static string[] RolesOf(ClaimsPrincipal p)
        => p.FindAll("role").Select(c => c.Value).ToArray();

    [Fact]
    public async Task Maps_realm_access_roles_to_app_roles()
    {
        var sut = new KeycloakClaimsTransformation(RoleMap);
        var principal = PrincipalWith(
            new Claim("sub", "kc-uuid-1"),
            new Claim("realm_access", RealmAccess("author", "approver", "reader")));

        var result = await sut.TransformAsync(principal);

        var roles = RolesOf(result);
        Assert.Contains(QgpRoles.Author, roles);
        Assert.Contains(QgpRoles.Approver, roles);
        Assert.Contains(QgpRoles.Reader, roles);
    }

    [Fact]
    public async Task Ignores_keycloak_roles_not_in_map()
    {
        var sut = new KeycloakClaimsTransformation(RoleMap);
        var principal = PrincipalWith(
            new Claim("realm_access", RealmAccess("author", "offline_access", "uma_authorization")));

        var roles = RolesOf(await sut.TransformAsync(principal));

        Assert.Equal([QgpRoles.Author], roles);
    }

    [Fact]
    public async Task No_realm_access_claim_adds_no_roles()
    {
        var sut = new KeycloakClaimsTransformation(RoleMap);
        var principal = PrincipalWith(new Claim("sub", "kc-uuid-2"));

        var roles = RolesOf(await sut.TransformAsync(principal));

        Assert.Empty(roles);
    }

    [Fact]
    public async Task Malformed_realm_access_does_not_throw_and_adds_no_roles()
    {
        var sut = new KeycloakClaimsTransformation(RoleMap);
        var principal = PrincipalWith(new Claim("realm_access", "not-json{"));

        var roles = RolesOf(await sut.TransformAsync(principal));

        Assert.Empty(roles);
    }

    [Fact]
    public async Task Is_idempotent_no_duplicate_role_claims()
    {
        var sut = new KeycloakClaimsTransformation(RoleMap);
        var principal = PrincipalWith(new Claim("realm_access", RealmAccess("admin")));

        await sut.TransformAsync(principal);
        var roles = RolesOf(await sut.TransformAsync(principal));

        Assert.Equal([QgpRoles.Admin], roles);
    }

    [Fact]
    public async Task Role_map_lookup_is_case_insensitive_on_keycloak_role()
    {
        var sut = new KeycloakClaimsTransformation(RoleMap);
        var principal = PrincipalWith(new Claim("realm_access", RealmAccess("Author", "ADMIN")));

        var roles = RolesOf(await sut.TransformAsync(principal));

        Assert.Contains(QgpRoles.Author, roles);
        Assert.Contains(QgpRoles.Admin, roles);
    }
}
