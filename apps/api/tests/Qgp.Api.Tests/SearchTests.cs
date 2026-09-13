using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Qgp.Api.Infrastructure.Persistence;
using Qgp.Api.Infrastructure.Search;

namespace Qgp.Api.Tests;

/// <summary>
/// Integration test Search (Meilisearch): bản Effective được index & tìm thấy;
/// bản Draft KHÔNG được index (BR-06). Cần Postgres + Meili (docker compose up).
/// </summary>
public class SearchTests(QgpApiFactory factory) : IClassFixture<QgpApiFactory>
{
    private readonly QgpApiFactory _factory = factory;

    private async Task<HttpClient> LoginAsync(params string[] roles)
    {
        var client = _factory.CreateClient();
        var res = await client.PostAsJsonAsync("/auth/dev-login", new { sub = "search@fpt", roles });
        res.EnsureSuccessStatusCode();
        var token = (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("access_token").GetString();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    [Fact]
    public async Task Effective_is_searchable_but_draft_is_not_indexed()
    {
        var client = await LoginAsync("AUTHOR", "APPROVER");
        var token = $"zqx{Guid.NewGuid():N}"[..12];               // từ khoá độc nhất
        var effDoc = $"IT-SRCH-E-{Guid.NewGuid():N}"[..22];
        var draftDoc = $"IT-SRCH-D-{Guid.NewGuid():N}"[..22];
        var today = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");

        try
        {
            // Doc 1: publish Effective (sẽ được index).
            (await client.PostAsJsonAsync("/v1/documents", new
            {
                doc_id = effDoc, title = $"Huong dan {token}", type = "Process", content_markdown = $"# {token}\nNoi dung",
            })).EnsureSuccessStatusCode();
            var vid = (await client.GetFromJsonAsync<JsonElement>($"/v1/documents/{effDoc}/versions"))
                .EnumerateArray().Single().GetProperty("id").GetString();
            (await client.PostAsync($"/v1/versions/{vid}/submit", null)).EnsureSuccessStatusCode();
            (await client.PostAsJsonAsync($"/v1/versions/{vid}/approve", new { decision = "approve" })).EnsureSuccessStatusCode();
            (await client.PostAsJsonAsync($"/v1/versions/{vid}/publish", new
            {
                issue_date = today, effective_date = today, change_summary = "init",
            })).EnsureSuccessStatusCode();

            // Doc 2: chỉ Draft (không publish → KHÔNG index — BR-06).
            (await client.PostAsJsonAsync("/v1/documents", new
            {
                doc_id = draftDoc, title = $"Nhap {token}", type = "Process", content_markdown = $"# {token}",
            })).EnsureSuccessStatusCode();

            // Meili index bất đồng bộ (task 202) → poll tới khi bản Effective xuất hiện (eventual
            // consistency), tối đa ~3s. Tránh flaky khi index chưa kịp catch-up ngay sau publish.
            var hits = new List<string?>();
            for (var i = 0; i < 15; i++)
            {
                var page = await client.GetFromJsonAsync<JsonElement>($"/v1/search?q={token}");
                hits = page.GetProperty("data").EnumerateArray()
                    .Select(h => h.GetProperty("doc_id").GetString()).ToList();
                if (hits.Contains(effDoc)) break;
                await Task.Delay(200);
            }

            Assert.Contains(effDoc, hits);       // Effective tìm thấy
            Assert.DoesNotContain(draftDoc, hits); // Draft KHÔNG index (BR-06)
        }
        finally
        {
            await CleanupAsync(effDoc);
            await CleanupAsync(draftDoc);
        }
    }

    private async Task CleanupAsync(string docId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<QgpDbContext>();
        var doc = db.Documents.FirstOrDefault(d => d.DocId == docId);
        if (doc is not null)
        {
            doc.CurrentEffectiveVersionId = null;
            await db.SaveChangesAsync();
            db.Documents.Remove(doc);
            await db.SaveChangesAsync();
        }
        try { await scope.ServiceProvider.GetRequiredService<ISearchIndex>().DeleteAsync(docId); } catch { /* best-effort */ }
    }
}
