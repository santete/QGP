using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Qgp.Api.Infrastructure.Persistence;

namespace Qgp.Api.Tests;

/// <summary>
/// Integration test REC engine (BR-11, SDD §5.2): gợi ý explainable theo role.
/// - Chỉ tài liệu Effective trong audience_roles khớp role của user (candidate).
/// - mandatory_ack chưa ack → must_read "Bắt buộc"; ngoài ra → suggested.
/// - Không thuộc audience role của user → KHÔNG xuất hiện.
/// - Ack rồi → rời must_read.
/// Cần Postgres (docker compose up). RBAC: doc.read (mọi role); no-role → 403.
/// </summary>
public class RecommendationTests(QgpApiFactory factory) : IClassFixture<QgpApiFactory>
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

    /// <summary>Tạo tài liệu → submit → approve → publish tới hạn (effective_date=today) → Effective ngay. Trả version id.</summary>
    private static async Task<string> PublishEffectiveAsync(
        HttpClient client, string docId, string title, bool mandatoryAck, string[] audienceRoles)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");
        (await client.PostAsJsonAsync("/v1/documents", new
        {
            doc_id = docId, title, type = "Process",
            mandatory_ack = mandatoryAck, audience_roles = audienceRoles,
            content_markdown = "# Noi dung",
        })).EnsureSuccessStatusCode();

        var vid = (await client.GetFromJsonAsync<JsonElement>($"/v1/documents/{docId}/versions"))
            .EnumerateArray().Single().GetProperty("id").GetString()!;
        (await client.PostAsync($"/v1/versions/{vid}/submit", null)).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync($"/v1/versions/{vid}/approve", new { decision = "approve" })).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync($"/v1/versions/{vid}/publish", new
        {
            issue_date = today, effective_date = today, change_summary = "init",
        })).EnsureSuccessStatusCode();
        return vid;
    }

    private static (List<string> mustRead, List<string> suggested, JsonElement root) Buckets(JsonElement recs)
    {
        List<string> ids(string bucket) => recs.TryGetProperty(bucket, out var arr) && arr.ValueKind == JsonValueKind.Array
            ? arr.EnumerateArray().Select(x => x.GetProperty("doc_id").GetString()!).ToList()
            : [];
        return (ids("must_read"), ids("suggested"), recs);
    }

    [Fact]
    public async Task No_role_token_is_forbidden()
    {
        var client = await LoginAsync("rec-norole@fpt"); // 0 role → policy doc.read fail
        var res = await client.GetAsync("/v1/recommendations");
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Buckets_respect_audience_role_and_mandatory_ack()
    {
        var client = await LoginAsync("rec-user@fpt", "AUTHOR", "APPROVER");
        var docA = $"IT-REC-A-{Guid.NewGuid():N}"[..20];  // audience APPROVER + mandatory → must_read
        var docB = $"IT-REC-B-{Guid.NewGuid():N}"[..20];  // audience APPROVER, không mandatory → suggested
        var docC = $"IT-REC-C-{Guid.NewGuid():N}"[..20];  // audience READER (user KHÔNG có) → absent
        try
        {
            await PublishEffectiveAsync(client, docA, "REC A", mandatoryAck: true, ["APPROVER"]);
            await PublishEffectiveAsync(client, docB, "REC B", mandatoryAck: false, ["APPROVER"]);
            await PublishEffectiveAsync(client, docC, "REC C", mandatoryAck: true, ["READER"]);

            var recs = await client.GetFromJsonAsync<JsonElement>("/v1/recommendations");
            var (mustRead, suggested, _) = Buckets(recs);

            Assert.Contains(docA, mustRead);       // mandatory chưa ack → bắt buộc
            Assert.DoesNotContain(docA, suggested);
            Assert.Contains(docB, suggested);      // khớp role, không mandatory
            Assert.DoesNotContain(docB, mustRead);
            Assert.DoesNotContain(docC, mustRead); // ngoài audience role của user
            Assert.DoesNotContain(docC, suggested);

            // reason explainable (BR-11) phải có với docA.
            var itemA = recs.GetProperty("must_read").EnumerateArray()
                .Single(x => x.GetProperty("doc_id").GetString() == docA);
            Assert.False(string.IsNullOrWhiteSpace(itemA.GetProperty("reason").GetString()));
        }
        finally
        {
            await CleanupAsync(docA, docB, docC);
        }
    }

    [Fact]
    public async Task Acknowledged_mandatory_doc_leaves_must_read()
    {
        var client = await LoginAsync("rec-ack@fpt", "AUTHOR", "APPROVER");
        var docId = $"IT-REC-K-{Guid.NewGuid():N}"[..20];
        try
        {
            var vid = await PublishEffectiveAsync(client, docId, "REC K", mandatoryAck: true, ["APPROVER"]);

            var before = Buckets(await client.GetFromJsonAsync<JsonElement>("/v1/recommendations"));
            Assert.Contains(docId, before.mustRead);

            (await client.PostAsync($"/v1/versions/{vid}/acknowledge", null)).EnsureSuccessStatusCode();

            var after = Buckets(await client.GetFromJsonAsync<JsonElement>("/v1/recommendations"));
            Assert.DoesNotContain(docId, after.mustRead); // đã ack → không còn bắt buộc
        }
        finally
        {
            await CleanupAsync(docId);
        }
    }

    private async Task CleanupAsync(params string[] docIds)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<QgpDbContext>();
        foreach (var docId in docIds)
        {
            var doc = db.Documents.FirstOrDefault(d => d.DocId == docId);
            if (doc is null) continue;
            doc.CurrentEffectiveVersionId = null;
            await db.SaveChangesAsync();
            db.Documents.Remove(doc);
            await db.SaveChangesAsync();
        }
    }
}
