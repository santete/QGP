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
/// Integration test Reports (RPT-F-01/02/04, S15): issuance aggregate + compliance ack.
/// RBAC admin.config (QA_LEAD/ADMIN); READER → 403. Cần Postgres.
/// </summary>
public class ReportTests(QgpApiFactory factory) : IClassFixture<QgpApiFactory>
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

    private static async Task<string> PublishEffectiveAsync(HttpClient client, string docId, string title, bool mandatory, string[] audience)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");
        (await client.PostAsJsonAsync("/v1/documents", new
        {
            doc_id = docId, title, type = "Process", mandatory_ack = mandatory, audience_roles = audience, content_markdown = "# x",
        })).EnsureSuccessStatusCode();
        var vid = (await client.GetFromJsonAsync<JsonElement>($"/v1/documents/{docId}/versions"))
            .EnumerateArray().Single().GetProperty("id").GetString()!;
        (await client.PostAsync($"/v1/versions/{vid}/submit", null)).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync($"/v1/versions/{vid}/approve", new { decision = "approve" })).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync($"/v1/versions/{vid}/publish", new { issue_date = today, effective_date = today, change_summary = "init" }))
            .EnsureSuccessStatusCode();
        return vid;
    }

    /// <summary>Gán role cho user (theo sso_subject) — mô phỏng audience thật cho compliance.</summary>
    private async Task AssignRoleAsync(string sub, string roleCode)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<QgpDbContext>();
        var userId = await db.Users.Where(u => u.SsoSubject == sub).Select(u => u.Id).FirstAsync();
        var roleId = await db.Roles.Where(r => r.Code == roleCode).Select(r => r.Id).FirstAsync();
        if (!await db.UserRoles.AnyAsync(ur => ur.UserId == userId && ur.RoleId == roleId))
        {
            db.UserRoles.Add(new UserRole { UserId = userId, RoleId = roleId });
            await db.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task Reader_forbidden_from_reports()
    {
        var client = await LoginAsync("rpt-reader@fpt", "READER");
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/v1/reports/issuance")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/v1/reports/compliance")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/v1/reports/feedback")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/v1/reports/access")).StatusCode);
    }

    [Fact]
    public async Task Access_and_feedback_reports_return_shape()
    {
        var client = await LoginAsync("rpt-eng@fpt", "QA_LEAD");
        var docId = $"IT-RPT-E-{Guid.NewGuid():N}"[..20];
        try
        {
            await PublishEffectiveAsync(client, docId, "Access doc", mandatory: false, ["QA_LEAD"]); // 0 ack → zero-ack

            var access = await client.GetFromJsonAsync<JsonElement>("/v1/reports/access");
            Assert.False(string.IsNullOrWhiteSpace(access.GetProperty("note").GetString()));
            var zeroAck = access.GetProperty("zero_ack_docs").EnumerateArray()
                .Select(x => x.GetProperty("doc_id").GetString()).ToList();
            Assert.Contains(docId, zeroAck);

            var fb = await client.GetFromJsonAsync<JsonElement>("/v1/reports/feedback");
            Assert.True(fb.GetProperty("total").GetInt32() >= 0);
            Assert.Equal(JsonValueKind.Object, fb.GetProperty("by_status").ValueKind);
        }
        finally { await CleanupAsync(docId); }
    }

    [Fact]
    public async Task Issuance_report_counts_effective_document()
    {
        var client = await LoginAsync("rpt-qa@fpt", "QA_LEAD");
        var docId = $"IT-RPT-I-{Guid.NewGuid():N}"[..20];
        try
        {
            await PublishEffectiveAsync(client, docId, "Issuance doc", mandatory: false, ["QA_LEAD"]);

            var rep = await client.GetFromJsonAsync<JsonElement>("/v1/reports/issuance");
            Assert.True(rep.GetProperty("total_documents").GetInt32() >= 1);
            Assert.True(rep.GetProperty("effective_count").GetInt32() >= 1);
            Assert.True(rep.GetProperty("by_version_status").TryGetProperty("Effective", out var eff) && eff.GetInt32() >= 1);
            Assert.True(rep.GetProperty("by_type").TryGetProperty("Process", out _));
        }
        finally { await CleanupAsync(docId); }
    }

    [Fact]
    public async Task Compliance_report_reflects_ack_in_audience()
    {
        var sub = "rpt-comp@fpt";
        var client = await LoginAsync(sub, "QA_LEAD");
        var docId = $"IT-RPT-C-{Guid.NewGuid():N}"[..20];
        try
        {
            var vid = await PublishEffectiveAsync(client, docId, "Compliance doc", mandatory: true, ["QA_LEAD"]);
            await AssignRoleAsync(sub, "QA_LEAD");                       // user thuộc audience
            (await client.PostAsync($"/v1/versions/{vid}/acknowledge", null)).EnsureSuccessStatusCode();

            var rep = await client.GetFromJsonAsync<JsonElement>("/v1/reports/compliance");
            var item = rep.GetProperty("items").EnumerateArray()
                .Single(x => x.GetProperty("doc_id").GetString() == docId);
            Assert.True(item.GetProperty("audience_count").GetInt32() >= 1);
            Assert.True(item.GetProperty("acked_count").GetInt32() >= 1);
            // Audience = mọi user có role QA_LEAD (có thể gồm user của test song song khác) → không assert 100%.
            // Bất biến ổn định: user vừa ack KHÔNG nằm trong not_read.
            var notRead = item.GetProperty("not_read").EnumerateArray().Select(x => x.GetString()).ToList();
            Assert.DoesNotContain(sub, notRead);
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
            db.Documents.Remove(doc);
            await db.SaveChangesAsync();
        }
    }
}
