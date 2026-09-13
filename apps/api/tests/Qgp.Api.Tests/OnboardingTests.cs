using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Qgp.Api.Domain.Entities;
using Qgp.Api.Infrastructure.Persistence;

namespace Qgp.Api.Tests;

/// <summary>
/// Integration test Onboarding (ONB-F-01/02/04, S12): lộ trình theo role + tiến độ đọc.
/// Testing env KHÔNG seed → test tự tạo LearningPath/PathItem qua DbContext. Cần Postgres.
/// </summary>
public class OnboardingTests(QgpApiFactory factory) : IClassFixture<QgpApiFactory>
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

    private static async Task<string> PublishEffectiveAsync(HttpClient client, string docId, string title)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");
        (await client.PostAsJsonAsync("/v1/documents", new { doc_id = docId, title, type = "Process", content_markdown = "# x" }))
            .EnsureSuccessStatusCode();
        var vid = (await client.GetFromJsonAsync<JsonElement>($"/v1/documents/{docId}/versions"))
            .EnumerateArray().Single().GetProperty("id").GetString()!;
        (await client.PostAsync($"/v1/versions/{vid}/submit", null)).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync($"/v1/versions/{vid}/approve", new { decision = "approve" })).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync($"/v1/versions/{vid}/publish", new { issue_date = today, effective_date = today, change_summary = "init" }))
            .EnsureSuccessStatusCode();
        return vid;
    }

    /// <summary>Tạo learning path cho 1 role (theo code) trỏ tới doc. Trả path id để cleanup.</summary>
    private async Task<Guid> SeedPathAsync(string roleCode, string docId, int seq, bool mandatory)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<QgpDbContext>();
        var roleId = await db.Roles.Where(r => r.Code == roleCode).Select(r => r.Id).FirstAsync();
        var docGuid = await db.Documents.Where(d => d.DocId == docId).Select(d => d.Id).FirstAsync();
        var path = new LearningPath { RoleId = roleId, Title = $"Path {roleCode}" };
        path.Items.Add(new PathItem { DocumentId = docGuid, Seq = seq, Mandatory = mandatory });
        db.LearningPaths.Add(path);
        await db.SaveChangesAsync();
        return path.Id;
    }

    [Fact]
    public async Task No_role_token_is_forbidden()
    {
        var client = await LoginAsync("onb-norole@fpt");
        var res = await client.GetAsync("/v1/onboarding");
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Path_lists_items_and_progress_reflects_ack()
    {
        var client = await LoginAsync("onb-user@fpt", "AUTHOR", "QA_LEAD");
        var docId = $"IT-ONB-{Guid.NewGuid():N}"[..18];
        Guid pathId = default;
        try
        {
            var vid = await PublishEffectiveAsync(client, docId, "Onboarding doc");
            pathId = await SeedPathAsync("QA_LEAD", docId, seq: 1, mandatory: true);

            var before = await client.GetFromJsonAsync<JsonElement>("/v1/onboarding");
            var itemsBefore = before.GetProperty("items").EnumerateArray().ToList();
            Assert.Contains(itemsBefore, x => x.GetProperty("doc_id").GetString() == docId);
            var item = itemsBefore.Single(x => x.GetProperty("doc_id").GetString() == docId);
            Assert.False(item.GetProperty("acked").GetBoolean());
            Assert.Equal(0, before.GetProperty("progress").GetProperty("percent").GetInt32());

            // Ack bản Effective → tiến độ tăng.
            (await client.PostAsync($"/v1/versions/{vid}/acknowledge", null)).EnsureSuccessStatusCode();

            var after = await client.GetFromJsonAsync<JsonElement>("/v1/onboarding");
            var itemAfter = after.GetProperty("items").EnumerateArray()
                .Single(x => x.GetProperty("doc_id").GetString() == docId);
            Assert.True(itemAfter.GetProperty("acked").GetBoolean());
            Assert.Equal(100, after.GetProperty("progress").GetProperty("percent").GetInt32());
        }
        finally
        {
            await CleanupAsync(docId, pathId);
        }
    }

    private async Task CleanupAsync(string docId, Guid pathId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<QgpDbContext>();
        if (pathId != default)
        {
            var path = db.LearningPaths.FirstOrDefault(p => p.Id == pathId);
            if (path is not null) { db.LearningPaths.Remove(path); await db.SaveChangesAsync(); }
        }
        var doc = db.Documents.FirstOrDefault(d => d.DocId == docId);
        if (doc is not null)
        {
            doc.CurrentEffectiveVersionId = null;
            await db.SaveChangesAsync();
            db.Documents.Remove(doc);
            await db.SaveChangesAsync();
        }
    }
}
