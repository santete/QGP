using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Qgp.Api.Domain.Entities;
using Qgp.Api.Infrastructure.Persistence;

namespace Qgp.Api.Tests;

/// <summary>
/// Integration test Notifications (ADM-F-04, S19): publish → Effective sinh thông báo cho audience;
/// list + mark-read. Cần Postgres. RBAC doc.read.
/// </summary>
public class NotificationTests(QgpApiFactory factory) : IClassFixture<QgpApiFactory>
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
    public async Task Publish_to_effective_notifies_audience_and_mark_read_works()
    {
        var sub = "notif-user@fpt";
        var client = await LoginAsync(sub, "QA_LEAD");
        var docId = $"IT-NOTIF-{Guid.NewGuid():N}"[..20];
        var today = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");
        try
        {
            // Tạo doc (audit → upsert user) rồi gán role QA_LEAD để user thuộc audience TRƯỚC khi publish.
            (await client.PostAsJsonAsync("/v1/documents", new
            {
                doc_id = docId, title = "Notif doc", type = "Process",
                mandatory_ack = true, audience_roles = new[] { "QA_LEAD" }, content_markdown = "# x",
            })).EnsureSuccessStatusCode();
            await AssignRoleAsync(sub, "QA_LEAD");

            var vid = (await client.GetFromJsonAsync<JsonElement>($"/v1/documents/{docId}/versions"))
                .EnumerateArray().Single().GetProperty("id").GetString()!;
            (await client.PostAsync($"/v1/versions/{vid}/submit", null)).EnsureSuccessStatusCode();
            (await client.PostAsJsonAsync($"/v1/versions/{vid}/approve", new { decision = "approve" })).EnsureSuccessStatusCode();
            (await client.PostAsJsonAsync($"/v1/versions/{vid}/publish", new { issue_date = today, effective_date = today, change_summary = "init" }))
                .EnsureSuccessStatusCode();

            // Thông báo được sinh cho audience (chính user này).
            var list = await client.GetFromJsonAsync<JsonElement>("/v1/notifications");
            Assert.True(list.GetProperty("unread_count").GetInt32() >= 1);
            var mine = list.GetProperty("data").EnumerateArray()
                .Where(n => n.TryGetProperty("doc_id", out var d) && d.GetString() == docId).ToList();
            Assert.NotEmpty(mine);
            var notifId = mine[0].GetProperty("id").GetString()!;
            Assert.False(mine[0].GetProperty("read").GetBoolean());

            // Đánh dấu đã đọc → read=true, unread_count giảm.
            var read = await (await client.PostAsync($"/v1/notifications/{notifId}/read", null)).Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(read.GetProperty("read").GetBoolean());

            var after = await client.GetFromJsonAsync<JsonElement>("/v1/notifications?unread_only=true");
            Assert.DoesNotContain(after.GetProperty("data").EnumerateArray(),
                n => n.GetProperty("id").GetString() == notifId);
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
            var docGuid = doc.Id;
            var notifs = db.Notifications.Where(n => n.DocumentId == docGuid);
            db.Notifications.RemoveRange(notifs);
            doc.CurrentEffectiveVersionId = null;
            await db.SaveChangesAsync();
            db.Documents.Remove(doc);
            await db.SaveChangesAsync();
        }
    }
}
