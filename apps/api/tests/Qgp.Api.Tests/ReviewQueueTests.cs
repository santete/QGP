using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Qgp.Api.Infrastructure.Persistence;

namespace Qgp.Api.Tests;

/// <summary>
/// Integration test hàng đợi duyệt S9 (GET /v1/review-queue): chỉ version InReview,
/// RBAC doc.approve, biến mất sau khi approve. Cần Postgres (docker compose up).
/// </summary>
public class ReviewQueueTests(QgpApiFactory factory) : IClassFixture<QgpApiFactory>
{
    private readonly QgpApiFactory _factory = factory;

    private async Task<HttpClient> LoginAsync(params string[] roles)
    {
        var client = _factory.CreateClient();
        var res = await client.PostAsJsonAsync("/auth/dev-login", new { sub = "tester@fpt", roles });
        res.EnsureSuccessStatusCode();
        var token = (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("access_token").GetString();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static async Task<string[]> QueueDocIdsAsync(HttpClient client, string status = "InReview")
    {
        var page = await client.GetFromJsonAsync<JsonElement>($"/v1/review-queue?status={status}&limit=200");
        return page.GetProperty("data").EnumerateArray()
            .Select(x => x.GetProperty("doc_id").GetString()!)
            .ToArray();
    }

    [Fact]
    public async Task Approved_version_appears_in_publish_queue()
    {
        var author = await LoginAsync("AUTHOR", "APPROVER");
        var docId = $"IT-PBQ-{Guid.NewGuid():N}"[..18];
        try
        {
            (await author.PostAsJsonAsync("/v1/documents", new
            {
                doc_id = docId,
                title = "Chờ ban hành",
                type = "Process",
                content_markdown = "# Draft",
            })).EnsureSuccessStatusCode();
            var versions = await author.GetFromJsonAsync<JsonElement>($"/v1/documents/{docId}/versions");
            var vid = versions.EnumerateArray().Single().GetProperty("id").GetString();

            (await author.PostAsync($"/v1/versions/{vid}/submit", null)).EnsureSuccessStatusCode();
            (await author.PostAsJsonAsync($"/v1/versions/{vid}/approve", new { decision = "approve" })).EnsureSuccessStatusCode();

            // Approved → có trong publish queue (status=Approved), KHÔNG còn ở InReview.
            Assert.Contains(docId, await QueueDocIdsAsync(author, "Approved"));
            Assert.DoesNotContain(docId, await QueueDocIdsAsync(author, "InReview"));
        }
        finally
        {
            await CleanupAsync(docId);
        }
    }

    [Fact]
    public async Task Submitted_version_appears_in_queue_then_gone_after_approve()
    {
        var author = await LoginAsync("AUTHOR", "APPROVER");
        var docId = $"IT-RVQ-{Guid.NewGuid():N}"[..18];

        try
        {
            (await author.PostAsJsonAsync("/v1/documents", new
            {
                doc_id = docId,
                title = "Tài liệu chờ duyệt",
                type = "Process",
                content_markdown = "# Draft",
            })).EnsureSuccessStatusCode();

            var versions = await author.GetFromJsonAsync<JsonElement>($"/v1/documents/{docId}/versions");
            var vid = versions.EnumerateArray().Single().GetProperty("id").GetString();

            // Draft: CHƯA có trong queue.
            Assert.DoesNotContain(docId, await QueueDocIdsAsync(author));

            // Submit → InReview → CÓ trong queue, kèm đủ field.
            (await author.PostAsync($"/v1/versions/{vid}/submit", null)).EnsureSuccessStatusCode();
            var page = await author.GetFromJsonAsync<JsonElement>("/v1/review-queue?limit=200");
            var item = page.GetProperty("data").EnumerateArray()
                .Single(x => x.GetProperty("doc_id").GetString() == docId);
            Assert.Equal(vid, item.GetProperty("version_id").GetString());
            Assert.Equal("Tài liệu chờ duyệt", item.GetProperty("title").GetString());
            Assert.Equal("1.0", item.GetProperty("version").GetString());
            Assert.False(string.IsNullOrEmpty(item.GetProperty("submitted_at").GetString()));

            // Approve → rời queue.
            (await author.PostAsJsonAsync($"/v1/versions/{vid}/approve", new { decision = "approve" }))
                .EnsureSuccessStatusCode();
            Assert.DoesNotContain(docId, await QueueDocIdsAsync(author));
        }
        finally
        {
            await CleanupAsync(docId);
        }
    }

    [Fact]
    public async Task Reader_cannot_read_review_queue_403()
    {
        var reader = await LoginAsync("READER");
        var res = await reader.GetAsync("/v1/review-queue");
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    private async Task CleanupAsync(string docId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<QgpDbContext>();
        var doc = db.Documents.FirstOrDefault(d => d.DocId == docId);
        if (doc is null) return;
        doc.CurrentEffectiveVersionId = null;
        await db.SaveChangesAsync();
        db.Documents.Remove(doc);
        await db.SaveChangesAsync();
    }
}
