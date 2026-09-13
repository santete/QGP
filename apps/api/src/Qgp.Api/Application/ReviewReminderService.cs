using Microsoft.EntityFrameworkCore;
using Qgp.Api.Auth;
using Qgp.Api.Domain.Entities;
using Qgp.Api.Infrastructure.Persistence;

namespace Qgp.Api.Application;

public interface IReviewReminderService
{
    /// <summary>
    /// Quét chu kỳ soát xét (DOC-F-08): với mỗi tài liệu có <c>next_review_date</c> trong
    /// khung cảnh báo (≤ LeadDaysWarn ngày, kể cả đã quá hạn), sinh thông báo cho user có role
    /// QA_LEAD/ADMIN (nguồn: user_roles — lớp chiếu B0). Trả về số thông báo đã tạo.
    /// Chống trùng: bỏ qua (doc, type) nếu đã có thông báo cùng loại trong LeadDaysWarn ngày gần nhất.
    /// </summary>
    Task<int> RunAsync(DateOnly asOf, CancellationToken ct = default);
}

/// <summary>
/// Nhắc soát xét tài liệu (DOC-F-08, "chống tài liệu chết già"). Chạy định kỳ qua Quartz
/// (<see cref="Infrastructure.Scheduling.ReviewReminderJob"/>) hoặc gọi trực tiếp (test).
/// </summary>
public sealed class ReviewReminderService(QgpDbContext db, IConfiguration config, ILogger<ReviewReminderService> logger)
    : IReviewReminderService
{
    public const string TypeDue = "doc_review_due";
    public const string TypeOverdue = "doc_review_overdue";

    public async Task<int> RunAsync(DateOnly asOf, CancellationToken ct = default)
    {
        var leadDays = Math.Max(1, config.GetValue("Review:LeadDaysWarn", 30));
        var warnCutoff = asOf.AddDays(leadDays);

        // Tài liệu cần nhắc: có next_review_date và đã vào cửa sổ cảnh báo (bao gồm quá hạn).
        var docs = await db.Documents.AsNoTracking()
            .Where(d => d.NextReviewDate != null && d.NextReviewDate <= warnCutoff)
            .Select(d => new { d.Id, d.DocId, ReviewDate = d.NextReviewDate!.Value })
            .ToListAsync(ct);
        if (docs.Count == 0) return 0;

        // Người nhận = user có role QA_LEAD/ADMIN (governance owner, RACI Accountable).
        var govRoleIds = await db.Roles.AsNoTracking()
            .Where(r => r.Code == QgpRoles.QaLead || r.Code == QgpRoles.Admin)
            .Select(r => r.Id)
            .ToListAsync(ct);
        var recipientIds = await db.UserRoles.AsNoTracking()
            .Where(ur => govRoleIds.Contains(ur.RoleId))
            .Select(ur => ur.UserId)
            .Distinct()
            .ToListAsync(ct);
        if (recipientIds.Count == 0)
        {
            logger.LogInformation("DOC-F-08: {DocCount} tài liệu tới hạn nhưng không có người nhận QA_LEAD/ADMIN.", docs.Count);
            return 0;
        }

        // Chống trùng: bỏ qua (doc, type) đã nhắc trong LeadDaysWarn ngày gần nhất (tránh spam mỗi ngày).
        var dedupeCutoff = DateTimeOffset.UtcNow.AddDays(-leadDays);
        var created = 0;
        foreach (var doc in docs)
        {
            var overdue = doc.ReviewDate < asOf;
            var type = overdue ? TypeOverdue : TypeDue;

            var alreadyNotified = await db.Notifications.AsNoTracking()
                .AnyAsync(n => n.DocumentId == doc.Id && n.Type == type && n.CreatedAt >= dedupeCutoff, ct);
            if (alreadyNotified) continue;

            var title = overdue
                ? $"Tài liệu {doc.DocId} đã QUÁ HẠN soát xét ({doc.ReviewDate:yyyy-MM-dd}) — cần rà soát ngay."
                : $"Tài liệu {doc.DocId} sắp tới hạn soát xét ({doc.ReviewDate:yyyy-MM-dd}) — vui lòng lên kế hoạch rà soát.";

            foreach (var userId in recipientIds)
            {
                db.Notifications.Add(new Notification
                {
                    UserId = userId,
                    Type = type,
                    DocumentId = doc.Id,
                    Title = title,
                });
                created++;
            }
        }

        if (created > 0)
        {
            await db.SaveChangesAsync(ct);
            logger.LogInformation("DOC-F-08: tạo {Count} thông báo nhắc soát xét (asOf={AsOf}, leadDays={Lead}).", created, asOf, leadDays);
        }
        return created;
    }
}
