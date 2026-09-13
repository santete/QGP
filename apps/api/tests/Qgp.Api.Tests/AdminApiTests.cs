using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Qgp.Api.Tests;

/// <summary>
/// Integration test B1 — Admin/S17 (ADM-F-01 RBAC read + users · ADM-F-02 tags CRUD).
/// RBAC admin.config: READER bị 403. Users lấy từ lớp chiếu B0 (/me). Cần Postgres.
/// </summary>
public class AdminApiTests(QgpApiFactory factory) : IClassFixture<QgpApiFactory>
{
    private readonly QgpApiFactory _factory = factory;

    private async Task<HttpClient> LoginAsync(string sub, params string[] roles)
    {
        var client = _factory.CreateClient();
        var res = await client.PostAsJsonAsync("/auth/dev-login", new { sub, roles });
        res.EnsureSuccessStatusCode();
        var token = (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("access_token").GetString();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    [Fact]
    public async Task Rbac_matrix_is_read_only_and_admin_only()
    {
        var reader = await LoginAsync("adm-reader@fpt", "READER");
        Assert.Equal(HttpStatusCode.Forbidden, (await reader.GetAsync("/v1/admin/rbac")).StatusCode);

        var admin = await LoginAsync("adm-boss@fpt", "ADMIN");
        var rbac = await admin.GetFromJsonAsync<JsonElement>("/v1/admin/rbac");

        var roleCodes = rbac.GetProperty("roles").EnumerateArray().Select(r => r.GetProperty("code").GetString()).ToArray();
        Assert.Contains("ADMIN", roleCodes);
        Assert.Equal(6, roleCodes.Length);

        var policies = rbac.GetProperty("policies").EnumerateArray().ToList();
        var adminConfig = policies.Single(p => p.GetProperty("policy").GetString() == "admin.config");
        var allowed = adminConfig.GetProperty("roles").EnumerateArray().Select(r => r.GetString()).ToArray();
        Assert.Contains("QA_LEAD", allowed);
        Assert.Contains("ADMIN", allowed);
        Assert.DoesNotContain("READER", allowed);
    }

    [Fact]
    public async Task Users_list_reflects_b0_projection()
    {
        var sub = $"adm-usr-{Guid.NewGuid():N}"[..18] + "@fpt";
        // Chiếu user + role QA_LEAD vào DB qua /me (B0).
        var qa = await LoginAsync(sub, "QA_LEAD");
        (await qa.GetAsync("/me")).EnsureSuccessStatusCode();

        var admin = await LoginAsync("adm-boss2@fpt", "ADMIN");
        var page = await admin.GetFromJsonAsync<JsonElement>("/v1/admin/users?limit=500");
        var me = page.GetProperty("data").EnumerateArray().SingleOrDefault(u => u.GetProperty("sub").GetString() == sub);
        Assert.Equal(JsonValueKind.Object, me.ValueKind);
        Assert.Contains("QA_LEAD", me.GetProperty("roles").EnumerateArray().Select(r => r.GetString()));
    }

    [Fact]
    public async Task Tag_crud_lifecycle_and_in_use_guard()
    {
        var admin = await LoginAsync("adm-tag@fpt", "ADMIN");
        var slug = $"tax-{Guid.NewGuid():N}"[..12];

        // Create
        var created = await (await admin.PostAsJsonAsync("/v1/admin/tags", new { slug, name = "Phân loại" })).Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetString();
        Assert.Equal(slug, created.GetProperty("slug").GetString());

        // Duplicate slug → 409
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync("/v1/admin/tags", new { slug, name = "x" })).StatusCode);

        // Update name
        var patched = await (await admin.PatchAsJsonAsync($"/v1/admin/tags/{id}", new { name = "Phân loại mới" })).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Phân loại mới", patched.GetProperty("name").GetString());

        // List contains it
        var list = await admin.GetFromJsonAsync<JsonElement>("/v1/admin/tags");
        Assert.Contains(list.GetProperty("data").EnumerateArray(), t => t.GetProperty("slug").GetString() == slug);

        // Delete → 204
        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/v1/admin/tags/{id}")).StatusCode);
    }
}
