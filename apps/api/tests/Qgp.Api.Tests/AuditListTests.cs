using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Qgp.Api.Infrastructure.Persistence;

namespace Qgp.Api.Tests;

/// <summary>
/// Audit list (E1.10, S16): GET /v1/audit — resolve actor từ actor_id, RBAC admin.config.
/// Cần Postgres (docker compose up).
/// </summary>
public class AuditListTests(QgpApiFactory factory) : IClassFixture<QgpApiFactory>
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
    public async Task Audit_list_shows_action_with_resolved_actor()
    {
        var sub = $"audit-viewer-{Guid.NewGuid():N}@fpt";
        var client = await LoginAsync(sub, "QA_LEAD"); // QA_LEAD có cả doc.author lẫn admin.config
        var docId = $"IT-AUL-{Guid.NewGuid():N}"[..18];

        try
        {
            (await client.PostAsJsonAsync("/v1/documents", new
            {
                doc_id = docId,
                title = "Audit list test",
                type = "Process",
                content_markdown = "# x",
            })).EnsureSuccessStatusCode();

            var page = await client.GetFromJsonAsync<JsonElement>("/v1/audit?limit=200");
            var entry = page.GetProperty("data").EnumerateArray()
                .FirstOrDefault(e => e.GetProperty("actor").GetString() == sub
                                     && e.GetProperty("action").GetString() == "document.created");

            Assert.Equal(JsonValueKind.Object, entry.ValueKind);           // tìm thấy
            Assert.Equal(sub, entry.GetProperty("actor").GetString());     // actor resolve từ actor_id
            Assert.False(string.IsNullOrEmpty(entry.GetProperty("at").GetString()));
        }
        finally
        {
            await CleanupAsync(docId);
        }
    }

    [Fact]
    public async Task Reader_cannot_read_audit_403()
    {
        var client = await LoginAsync("reader-x@fpt", "READER");
        var res = await client.GetAsync("/v1/audit");
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    private async Task CleanupAsync(string docId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<QgpDbContext>();
        var doc = db.Documents.FirstOrDefault(d => d.DocId == docId);
        if (doc is null) return;
        var logs = db.AuditLogs.Where(a => a.DocumentId == doc.Id);
        db.AuditLogs.RemoveRange(logs);
        doc.CurrentEffectiveVersionId = null;
        await db.SaveChangesAsync();
        db.Documents.Remove(doc);
        await db.SaveChangesAsync();
    }
}
