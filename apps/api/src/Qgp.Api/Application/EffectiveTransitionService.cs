using Microsoft.EntityFrameworkCore;
using Qgp.Api.Domain.Enums;
using Qgp.Api.Infrastructure.Persistence;

namespace Qgp.Api.Application;

public interface IEffectiveTransitionService
{
    /// <summary>Chuyển mọi version Published có effective_date &lt;= asOf sang Effective (WF-03). Trả số version đã chuyển.</summary>
    Task<int> RunAsync(DateOnly asOf, CancellationToken ct = default);
}

/// <summary>
/// Scheduler WF-03 (SDD §5.1): quét bản Published tới hạn → Effective + supersede (idempotent).
/// Chạy lại không tạo hiệu ứng phụ (bản đã Effective bị bỏ qua); partial unique index là chốt chặn cuối.
/// </summary>
public sealed class EffectiveTransitionService(QgpDbContext db, IAuditService audit, ISearchService search, INotificationService notifications, ILogger<EffectiveTransitionService> logger)
    : IEffectiveTransitionService
{
    public async Task<int> RunAsync(DateOnly asOf, CancellationToken ct = default)
    {
        var due = await db.DocumentVersions
            .Include(v => v.Document).ThenInclude(d => d.Versions)
            .Where(v => v.Status == VersionStatus.Published && v.EffectiveDate != null && v.EffectiveDate <= asOf)
            .OrderBy(v => v.EffectiveDate).ThenBy(v => v.Version)
            .ToListAsync(ct);

        if (due.Count == 0) return 0;

        var now = DateTimeOffset.UtcNow;
        foreach (var v in due)
        {
            EffectiveTransition.Apply(v.Document, v, now);
            // Hành động hệ thống (scheduler) → actor_id null; action đã ghi rõ "auto_effective".
            await audit.LogAsync(actorSub: null, $"version.auto_effective {v.Version}", v.DocumentId, ct);
            // Thông báo audience (ADM-F-04, S19).
            await notifications.CreateForAudienceAsync(v.DocumentId, v.Document.DocId, v.Version, ct);
        }

        await db.SaveChangesAsync(ct);

        // Đồng bộ index cho các tài liệu vừa Effective (BR-06). Best-effort.
        foreach (var docId in due.Select(v => v.Document.DocId).Distinct())
            await search.IndexEffectiveAsync(docId, ct);

        logger.LogInformation("WF-03: chuyển {Count} version sang Effective (asOf={AsOf})", due.Count, asOf);
        return due.Count;
    }
}
