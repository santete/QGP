using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Qgp.Api.Infrastructure.Persistence;

namespace Qgp.Api.Tests;

/// <summary>
/// Feedback (E1.9): create in-context (S13) + list/triage (S14). Server gắn version_id (FBK-F-04).
/// Cần Postgres (docker compose up).
/// </summary>
public class FeedbackTests(QgpApiFactory factory) : IClassFixture<QgpApiFactory>
{
    private readonly QgpApiFactory _factory = factory;

    private async Task<HttpClient> LoginAsync(params string[] roles)
    {
        var client = _factory.CreateClient();
        var res = await client.PostAsJsonAsync("/auth/dev-login", new { sub = "fb-tester@fpt", roles });
        res.EnsureSuccessStatusCode();
        var token = (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("access_token").GetString();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    [Fact]
    public async Task Create_list_triage_feedback_flow()
    {
        var client = await LoginAsync("QA_LEAD"); // doc.author + doc.read + admin.config
        var docId = $"IT-FBK-{Guid.NewGuid():N}"[..18];

        try
        {
            (await client.PostAsJsonAsync("/v1/documents", new
            {
                doc_id = docId, title = "Feedback doc", type = "Process", content_markdown = "# x",
            })).EnsureSuccessStatusCode();

            // S13: gửi feedback in-context (server gắn version_id từ doc_id+version).
            var create = await client.PostAsJsonAsync("/v1/feedback", new
            {
                doc_id = docId, version = "1.0", category = "content_error", body = "Sai ở bước 3",
            });
            Assert.Equal(HttpStatusCode.Created, create.StatusCode);
            var fb = await create.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("content_error", fb.GetProperty("category").GetString()); // map enum→snake
            Assert.Equal("New", fb.GetProperty("status").GetString());
            Assert.False(string.IsNullOrEmpty(fb.GetProperty("version_id").GetString()));
            var fid = fb.GetProperty("id").GetString();

            // S14: list để triage.
            var list = await client.GetFromJsonAsync<JsonElement>($"/v1/feedback?doc_id={docId}");
            var item = list.GetProperty("data").EnumerateArray().Single();
            Assert.Equal(docId, item.GetProperty("doc_id").GetString());
            Assert.Equal("Sai ở bước 3", item.GetProperty("body").GetString());

            // Triage → Triaged.
            var triage = await client.PatchAsJsonAsync($"/v1/feedback/{fid}", new { status = "Triaged", note = "Đã tiếp nhận" });
            triage.EnsureSuccessStatusCode();
            Assert.Equal("Triaged", (await triage.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetString());
        }
        finally
        {
            await CleanupAsync(docId);
        }
    }

    [Fact]
    public async Task Reader_can_create_but_cannot_list_feedback()
    {
        var reader = await LoginAsync("READER");
        // READER list → 403 (triage là quyền QA).
        Assert.Equal(HttpStatusCode.Forbidden, (await reader.GetAsync("/v1/feedback")).StatusCode);
    }

    private async Task CleanupAsync(string docId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<QgpDbContext>();
        var doc = await db.Documents.Include(d => d.Versions).FirstOrDefaultAsync(d => d.DocId == docId);
        if (doc is null) return;
        var vids = doc.Versions.Select(v => v.Id).ToList();
        db.Feedback.RemoveRange(db.Feedback.Where(f => vids.Contains(f.VersionId)));
        db.AuditLogs.RemoveRange(db.AuditLogs.Where(a => a.DocumentId == doc.Id));
        doc.CurrentEffectiveVersionId = null;
        await db.SaveChangesAsync();
        db.Documents.Remove(doc);
        await db.SaveChangesAsync();
    }
}
