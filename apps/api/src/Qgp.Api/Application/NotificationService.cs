using Microsoft.EntityFrameworkCore;
using Qgp.Api.Contracts;
using Qgp.Api.Domain.Entities;
using Qgp.Api.Infrastructure.Persistence;

namespace Qgp.Api.Application;

public interface INotificationService
{
    /// <summary>
    /// Sinh thông báo cho mọi user thuộc audience role của tài liệu khi 1 version thành Effective (ADM-F-04).
    /// KHÔNG gọi SaveChanges — caller commit chung (giống AuditService.LogAsync).
    /// </summary>
    Task CreateForAudienceAsync(Guid documentId, string docId, string versionLabel, CancellationToken ct = default);

    Task<NotificationListDto> ListAsync(string? sub, bool unreadOnly, int limit, CancellationToken ct = default);
    Task<NotificationDto> MarkReadAsync(Guid id, string? sub, CancellationToken ct = default);
    Task<int> MarkAllReadAsync(string? sub, CancellationToken ct = default);
}

/// <summary>Thông báo trong ứng dụng (ADM-F-04, S19). Nguồn: tài liệu trong audience của user trở thành Effective.</summary>
public sealed class NotificationService(QgpDbContext db) : INotificationService
{
    public async Task CreateForAudienceAsync(Guid documentId, string docId, string versionLabel, CancellationToken ct = default)
    {
        // Người nhận = audience (role trong doc_audience_roles) ∪ người theo dõi tài liệu (S19). Dedupe.
        var roleIds = await db.DocAudienceRoles.AsNoTracking()
            .Where(a => a.DocumentId == documentId)
            .Select(a => a.RoleId)
            .ToListAsync(ct);

        var audienceUserIds = roleIds.Count == 0
            ? new List<Guid>()
            : await db.UserRoles.AsNoTracking()
                .Where(ur => roleIds.Contains(ur.RoleId))
                .Select(ur => ur.UserId)
                .ToListAsync(ct);

        var subscriberIds = await db.Subscriptions.AsNoTracking()
            .Where(s => s.DocumentId == documentId)
            .Select(s => s.UserId)
            .ToListAsync(ct);

        var userIds = audienceUserIds.Concat(subscriberIds).Distinct().ToList();
        if (userIds.Count == 0) return;

        var title = $"Tài liệu {docId} đã ban hành bản {versionLabel} — vui lòng đọc & xác nhận.";
        foreach (var userId in userIds)
        {
            db.Notifications.Add(new Notification
            {
                UserId = userId,
                Type = "doc_effective",
                DocumentId = documentId,
                Title = title,
            });
        }
    }

    public async Task<NotificationListDto> ListAsync(string? sub, bool unreadOnly, int limit, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(sub))
            return new NotificationListDto([], 0);

        var userId = await db.Users.AsNoTracking()
            .Where(u => u.SsoSubject == sub)
            .Select(u => (Guid?)u.Id)
            .FirstOrDefaultAsync(ct);
        if (userId is null)
            return new NotificationListDto([], 0);

        var baseQuery = db.Notifications.AsNoTracking().Where(n => n.UserId == userId);
        var unreadCount = await baseQuery.CountAsync(n => n.ReadAt == null, ct);

        var q = unreadOnly ? baseQuery.Where(n => n.ReadAt == null) : baseQuery;
        var items = await q
            .OrderBy(n => n.ReadAt == null ? 0 : 1)   // chưa đọc trước
            .ThenByDescending(n => n.CreatedAt)
            .Take(Math.Clamp(limit, 1, 100))
            .Select(n => new NotificationDto(
                n.Id,
                n.Type,
                n.DocumentId == null ? null : db.Documents.Where(d => d.Id == n.DocumentId).Select(d => d.DocId).FirstOrDefault(),
                n.Title,
                n.ReadAt != null,
                n.CreatedAt))
            .ToListAsync(ct);

        return new NotificationListDto(items, unreadCount);
    }

    public async Task<NotificationDto> MarkReadAsync(Guid id, string? sub, CancellationToken ct = default)
    {
        var n = await db.Notifications.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw AppException.NotFound("NOTIFICATION_NOT_FOUND", "Không tìm thấy thông báo");

        // Chỉ chủ sở hữu mới đánh dấu đọc (defense-in-depth).
        var owner = await db.Users.AsNoTracking().Where(u => u.Id == n.UserId).Select(u => u.SsoSubject).FirstOrDefaultAsync(ct);
        if (owner != sub)
            throw AppException.NotFound("NOTIFICATION_NOT_FOUND", "Không tìm thấy thông báo");

        if (n.ReadAt is null)
        {
            n.ReadAt = DateTimeOffset.UtcNow;
            n.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
        }
        var docId = n.DocumentId is { } d ? await db.Documents.AsNoTracking().Where(x => x.Id == d).Select(x => x.DocId).FirstOrDefaultAsync(ct) : null;
        return new NotificationDto(n.Id, n.Type, docId, n.Title, true, n.CreatedAt);
    }

    public async Task<int> MarkAllReadAsync(string? sub, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(sub)) return 0;
        var userId = await db.Users.AsNoTracking().Where(u => u.SsoSubject == sub).Select(u => (Guid?)u.Id).FirstOrDefaultAsync(ct);
        if (userId is null) return 0;

        var now = DateTimeOffset.UtcNow;
        return await db.Notifications
            .Where(n => n.UserId == userId && n.ReadAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.ReadAt, now).SetProperty(n => n.UpdatedAt, now), ct);
    }
}
