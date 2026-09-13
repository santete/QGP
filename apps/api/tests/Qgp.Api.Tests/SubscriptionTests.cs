using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Qgp.Api.Infrastructure.Persistence;

namespace Qgp.Api.Tests;

/// <summary>
/// Integration test Subscription (ADM-F-04, S19): theo dõi tài liệu → nhận thông báo khi bản Effective mới;
/// toggle subscribe/unsubscribe. Cần Postgres.
/// </summary>
public class SubscriptionTests(QgpApiFactory factory) : IClassFixture<QgpApiFactory>
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
    public async Task Subscribe_then_publish_notifies_subscriber_even_outside_audience()
    {
        // Author tạo + publish; subscriber KHÔNG thuộc audience role nhưng theo dõi → vẫn nhận thông báo.
        var author = await LoginAsync("sub-author@fpt", "QA_LEAD");
        var subscriber = await LoginAsync("sub-follower@fpt", "READER");
        var docId = $"IT-SUB-{Guid.NewGuid():N}"[..18];
        var today = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");
        try
        {
            // Tạo doc audience CHỈ QA_LEAD (subscriber là READER → ngoài audience).
            (await author.PostAsJsonAsync("/v1/documents", new
            {
                doc_id = docId, title = "Sub doc", type = "Process", audience_roles = new[] { "QA_LEAD" }, content_markdown = "# x",
            })).EnsureSuccessStatusCode();

            // Subscriber theo dõi.
            var st = await (await subscriber.PostAsync($"/v1/documents/{docId}/subscribe", null)).Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(st.GetProperty("subscribed").GetBoolean());
            var subs = await subscriber.GetFromJsonAsync<JsonElement>("/v1/subscriptions");
            Assert.Contains(subs.GetProperty("data").EnumerateArray(), x => x.GetProperty("doc_id").GetString() == docId);

            // Author submit→approve→publish (Effective) → sinh thông báo cho subscriber.
            var vid = (await author.GetFromJsonAsync<JsonElement>($"/v1/documents/{docId}/versions"))
                .EnumerateArray().Single().GetProperty("id").GetString()!;
            (await author.PostAsync($"/v1/versions/{vid}/submit", null)).EnsureSuccessStatusCode();
            (await author.PostAsJsonAsync($"/v1/versions/{vid}/approve", new { decision = "approve" })).EnsureSuccessStatusCode();
            (await author.PostAsJsonAsync($"/v1/versions/{vid}/publish", new { issue_date = today, effective_date = today, change_summary = "init" }))
                .EnsureSuccessStatusCode();

            var notifs = await subscriber.GetFromJsonAsync<JsonElement>("/v1/notifications");
            Assert.Contains(notifs.GetProperty("data").EnumerateArray(),
                n => n.TryGetProperty("doc_id", out var d) && d.GetString() == docId);

            // Bỏ theo dõi → không còn trong danh sách.
            var un = await (await subscriber.DeleteAsync($"/v1/documents/{docId}/subscribe")).Content.ReadFromJsonAsync<JsonElement>();
            Assert.False(un.GetProperty("subscribed").GetBoolean());
            var subs2 = await subscriber.GetFromJsonAsync<JsonElement>("/v1/subscriptions");
            Assert.DoesNotContain(subs2.GetProperty("data").EnumerateArray(), x => x.GetProperty("doc_id").GetString() == docId);
        }
        finally { await CleanupAsync(docId); }
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
            db.Documents.Remove(doc); // cascade: subscriptions + notifications theo doc
            await db.SaveChangesAsync();
        }
    }
}
