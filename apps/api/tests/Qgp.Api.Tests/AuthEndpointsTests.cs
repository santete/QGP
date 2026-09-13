using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Qgp.Api.Tests;

/// <summary>
/// Integration test auth/RBAC (T6) — WebApplicationFactory, KHÔNG chạm DB.
/// Kiểm cả "có quyền" và "không có quyền" (SECURITY_RULES / Authorization).
/// </summary>
public class AuthEndpointsTests(QgpApiFactory factory)
    : IClassFixture<QgpApiFactory>
{
    private readonly QgpApiFactory _factory = factory;

    private async Task<HttpClient> LoginAsAsync(string sub, params string[] roles)
    {
        var client = _factory.CreateClient();
        var res = await client.PostAsJsonAsync("/auth/dev-login", new { sub, roles });
        res.EnsureSuccessStatusCode();
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        var token = body.GetProperty("access_token").GetString();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    [Fact]
    public async Task Me_without_token_returns_401()
    {
        var client = _factory.CreateClient();
        var res = await client.GetAsync("/me");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task DevLogin_then_Me_returns_sub_and_roles()
    {
        var client = await LoginAsAsync("alice@fpt", "AUTHOR", "APPROVER");
        var me = await client.GetFromJsonAsync<JsonElement>("/me");

        Assert.Equal("alice@fpt", me.GetProperty("sub").GetString());
        var roles = me.GetProperty("roles").EnumerateArray().Select(r => r.GetString()).ToArray();
        Assert.Contains("AUTHOR", roles);
        Assert.Contains("APPROVER", roles);
    }

    [Fact]
    public async Task AdminPing_forbidden_for_reader_403()
    {
        var client = await LoginAsAsync("bob@fpt", "READER");
        var res = await client.GetAsync("/admin/ping");
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task AdminPing_ok_for_admin_200()
    {
        var client = await LoginAsAsync("carol@fpt", "ADMIN");
        var res = await client.GetAsync("/admin/ping");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task DevLogin_rejects_unknown_role_422()
    {
        var client = _factory.CreateClient();
        var res = await client.PostAsJsonAsync("/auth/dev-login", new { sub = "x", roles = new[] { "SUPERUSER" } });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, res.StatusCode);
    }
}
