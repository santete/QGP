using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Qgp.Api.Application;
using Qgp.Api.Infrastructure.Persistence;

namespace Qgp.Api.Tests;

/// <summary>
/// Integration test scheduler WF-03 (EffectiveTransitionService): Published tới hạn → Effective,
/// supersede giữ đúng 1 Effective (BR-02), idempotent. Cần Postgres (docker compose up).
/// </summary>
public class SchedulerTests(QgpApiFactory factory) : IClassFixture<QgpApiFactory>
{
    private readonly QgpApiFactory _factory = factory;

    private async Task<HttpClient> LoginAsync(params string[] roles)
    {
        var client = _factory.CreateClient();
        var res = await client.PostAsJsonAsync("/auth/dev-login", new { sub = "sched@fpt", roles });
        res.EnsureSuccessStatusCode();
        var token = (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("access_token").GetString();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    [Fact]
    public async Task Published_future_becomes_effective_when_due_and_is_idempotent()
    {
        var client = await LoginAsync("AUTHOR", "APPROVER");
        var docId = $"IT-SCHED-{Guid.NewGuid():N}"[..22];
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var tomorrow = today.AddDays(1);

        try
        {
            (await client.PostAsJsonAsync("/v1/documents", new
            {
                doc_id = docId, title = "Sched", type = "Process", content_markdown = "# x",
            })).EnsureSuccessStatusCode();

            var versions = await client.GetFromJsonAsync<JsonElement>($"/v1/documents/{docId}/versions");
            var vid = versions.EnumerateArray().Single().GetProperty("id").GetString();

            (await client.PostAsync($"/v1/versions/{vid}/submit", null)).EnsureSuccessStatusCode();
            (await client.PostAsJsonAsync($"/v1/versions/{vid}/approve", new { decision = "approve" })).EnsureSuccessStatusCode();
            (await client.PostAsJsonAsync($"/v1/versions/{vid}/publish", new
            {
                issue_date = today.ToString("yyyy-MM-dd"),
                effective_date = tomorrow.ToString("yyyy-MM-dd"),
                change_summary = "future",
            })).EnsureSuccessStatusCode();

            // Chưa tới hạn (asOf=hôm nay) → chưa Effective.
            var before = await client.GetFromJsonAsync<JsonElement>($"/v1/documents/{docId}");
            Assert.False(before.GetProperty("is_effective").GetBoolean());

            // Scheduler chạy với asOf = ngày mai → chuyển Effective.
            var moved = await RunSchedulerAsync(tomorrow);
            Assert.True(moved >= 1);

            var after = await client.GetFromJsonAsync<JsonElement>($"/v1/documents/{docId}");
            Assert.True(after.GetProperty("is_effective").GetBoolean());
            Assert.Equal("Effective", after.GetProperty("status").GetString());

            // Idempotent: chạy lại vẫn đúng 1 Effective cho doc này.
            await RunSchedulerAsync(tomorrow);
            var vers2 = await client.GetFromJsonAsync<JsonElement>($"/v1/documents/{docId}/versions");
            var effectiveCount = vers2.EnumerateArray().Count(v => v.GetProperty("status").GetString() == "Effective");
            Assert.Equal(1, effectiveCount);
        }
        finally
        {
            await CleanupAsync(docId);
        }
    }

    private async Task<int> RunSchedulerAsync(DateOnly asOf)
    {
        using var scope = _factory.Services.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<IEffectiveTransitionService>();
        return await svc.RunAsync(asOf);
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
