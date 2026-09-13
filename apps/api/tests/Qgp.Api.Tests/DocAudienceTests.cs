using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Qgp.Api.Infrastructure.Persistence;

namespace Qgp.Api.Tests;

/// <summary>
/// Integration test B1 — doc audience roles (ADM-F-02, S17). GET/PUT /v1/documents/{docId}/audience.
/// PUT thay thế toàn bộ tập audience (reconcile). RBAC admin.config. Cần Postgres.
/// </summary>
public class DocAudienceTests(QgpApiFactory factory) : IClassFixture<QgpApiFactory>
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
    public async Task Set_and_get_audience_replaces_set_and_validates_roles()
    {
        // Author (QA_LEAD có cả doc.author lẫn admin.config) tạo doc rồi cấu hình audience.
        var client = await LoginAsync("aud-admin@fpt", "QA_LEAD");
        var docId = $"IT-AUD-{Guid.NewGuid():N}"[..20];
        try
        {
            (await client.PostAsJsonAsync("/v1/documents", new
            {
                doc_id = docId, title = "Aud doc", type = "Process",
                audience_roles = new[] { "READER" }, content_markdown = "# x",
            })).EnsureSuccessStatusCode();

            // Đặt lại audience = {AUTHOR(reason), APPROVER}.
            var put = await client.PutAsJsonAsync($"/v1/documents/{docId}/audience", new
            {
                audience = new[]
                {
                    new { role = "AUTHOR", reason = (string?)"Soạn thảo" },
                    new { role = "APPROVER", reason = (string?)null },
                },
            });
            put.EnsureSuccessStatusCode();

            var after = await client.GetFromJsonAsync<JsonElement>($"/v1/documents/{docId}/audience");
            var roles = after.GetProperty("data").EnumerateArray().Select(a => a.GetProperty("role").GetString()).OrderBy(r => r).ToArray();
            Assert.Equal(new[] { "APPROVER", "AUTHOR" }, roles); // READER cũ đã bị thay thế
            var author = after.GetProperty("data").EnumerateArray().Single(a => a.GetProperty("role").GetString() == "AUTHOR");
            Assert.Equal("Soạn thảo", author.GetProperty("reason").GetString());

            // Role không hợp lệ → 422.
            var bad = await client.PutAsJsonAsync($"/v1/documents/{docId}/audience", new { audience = new[] { new { role = "SUPERUSER", reason = (string?)null } } });
            Assert.Equal(HttpStatusCode.UnprocessableEntity, bad.StatusCode);
        }
        finally { await CleanupAsync(docId); }
    }

    [Fact]
    public async Task Audience_endpoints_require_admin_config()
    {
        var reader = await LoginAsync("aud-reader@fpt", "READER");
        Assert.Equal(HttpStatusCode.Forbidden, (await reader.GetAsync("/v1/documents/whatever/audience")).StatusCode);
    }

    private async Task CleanupAsync(string docId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<QgpDbContext>();
        var doc = await db.Documents.FirstOrDefaultAsync(d => d.DocId == docId);
        if (doc is null) return;
        doc.CurrentEffectiveVersionId = null;
        await db.SaveChangesAsync();
        db.Documents.Remove(doc);
        await db.SaveChangesAsync();
    }
}
