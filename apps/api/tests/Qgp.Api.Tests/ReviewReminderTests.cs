using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Qgp.Api.Application;
using Qgp.Api.Infrastructure.Persistence;

namespace Qgp.Api.Tests;

/// <summary>
/// Integration test B2 — Review-cycle reminders (DOC-F-08). ReviewReminderService quét
/// documents.next_review_date: ≤30 ngày → doc_review_due; quá hạn → doc_review_overdue;
/// người nhận = user có role QA_LEAD/ADMIN (từ lớp chiếu B0). Dedupe: chạy lại không nhân đôi.
/// Cần Postgres (docker compose up).
/// </summary>
public class ReviewReminderTests(QgpApiFactory factory) : IClassFixture<QgpApiFactory>
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

    private async Task CreateDocAsync(HttpClient client, string docId, DateOnly nextReview)
    {
        var res = await client.PostAsJsonAsync("/v1/documents", new
        {
            doc_id = docId,
            title = "Review cycle doc",
            type = "Process",
            next_review_date = nextReview.ToString("yyyy-MM-dd"),
            content_markdown = "# x",
        });
        res.EnsureSuccessStatusCode();
    }

    private async Task<int> RunReminderAsync(DateOnly asOf)
    {
        using var scope = _factory.Services.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<IReviewReminderService>();
        return await svc.RunAsync(asOf);
    }

    [Fact]
    public async Task Reminds_qa_lead_for_due_and_overdue_only_and_is_idempotent()
    {
        var sub = $"rev-{Guid.NewGuid():N}"[..16] + "@fpt";
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var docDue = $"IT-REVDUE-{suffix}";
        var docOverdue = $"IT-REVOD-{suffix}";
        var docFar = $"IT-REVFAR-{suffix}";
        var asOf = DateOnly.FromDateTime(DateTime.UtcNow);

        try
        {
            // Recipient = QA_LEAD (chiếu vào user_roles qua /me — dùng B0).
            var qa = await LoginAsync(sub, "QA_LEAD");
            (await qa.GetAsync("/me")).EnsureSuccessStatusCode();

            await CreateDocAsync(qa, docDue, asOf.AddDays(10));    // sắp tới hạn (≤30d)
            await CreateDocAsync(qa, docOverdue, asOf.AddDays(-5)); // quá hạn
            await CreateDocAsync(qa, docFar, asOf.AddDays(60));    // chưa tới cửa sổ 30d

            var created = await RunReminderAsync(asOf);
            Assert.True(created >= 2);

            var byDoc = await ReviewNotifsForAsync(sub, docDue, docOverdue, docFar);
            Assert.Equal("doc_review_due", byDoc.GetValueOrDefault(docDue));
            Assert.Equal("doc_review_overdue", byDoc.GetValueOrDefault(docOverdue));
            Assert.False(byDoc.ContainsKey(docFar)); // ngoài cửa sổ → không nhắc

            // Dedupe: chạy lại trong cùng cửa sổ → không tạo thêm cho các doc này.
            await RunReminderAsync(asOf);
            var after = await ReviewNotifsForAsync(sub, docDue, docOverdue, docFar);
            Assert.Equal(2, after.Count);
        }
        finally
        {
            await CleanupAsync(docDue, docOverdue, docFar);
        }
    }

    /// <summary>Map doc_id → type của thông báo review (doc_review_*) gửi cho <paramref name="sub"/>.</summary>
    private async Task<Dictionary<string, string>> ReviewNotifsForAsync(string sub, params string[] docIds)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<QgpDbContext>();
        var q =
            from n in db.Notifications.AsNoTracking()
            join u in db.Users.AsNoTracking() on n.UserId equals u.Id
            join d in db.Documents.AsNoTracking() on n.DocumentId equals d.Id
            where u.SsoSubject == sub && n.Type.StartsWith("doc_review_") && docIds.Contains(d.DocId)
            select new { d.DocId, n.Type };
        var rows = await q.ToListAsync();
        return rows.GroupBy(r => r.DocId).ToDictionary(g => g.Key, g => g.First().Type);
    }

    private async Task CleanupAsync(params string[] docIds)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<QgpDbContext>();
        foreach (var docId in docIds)
        {
            var doc = await db.Documents.FirstOrDefaultAsync(d => d.DocId == docId);
            if (doc is null) continue;
            var notifs = db.Notifications.Where(n => n.DocumentId == doc.Id);
            db.Notifications.RemoveRange(notifs);
            doc.CurrentEffectiveVersionId = null;
            await db.SaveChangesAsync();
            db.Documents.Remove(doc);
            await db.SaveChangesAsync();
        }
    }
}
