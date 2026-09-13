using Microsoft.EntityFrameworkCore;
using Qgp.Api.Contracts;
using Qgp.Api.Domain.Enums;
using Qgp.Api.Infrastructure.Persistence;

namespace Qgp.Api.Application;

public interface IReportService
{
    /// <summary>Báo cáo ban hành + sức khoẻ tài liệu (RPT-F-01/02). Aggregate.</summary>
    Task<IssuanceReportDto> GetIssuanceAsync(CancellationToken ct = default);

    /// <summary>Compliance acknowledgement theo tài liệu bắt buộc (RPT-F-04). Ai chưa đọc = trong audience role.</summary>
    Task<ComplianceReportDto> GetComplianceAsync(CancellationToken ct = default);

    /// <summary>Thống kê feedback (RPT-F-05) — theo trạng thái + thời gian xử lý TB.</summary>
    Task<FeedbackReportDto> GetFeedbackAsync(CancellationToken ct = default);

    /// <summary>Truy cập proxy (RPT-F-03) — theo lượt ack (chưa có event lượt xem). Aggregate.</summary>
    Task<AccessReportDto> GetAccessAsync(CancellationToken ct = default);
}

/// <summary>
/// Báo cáo quản trị (RPT, S15). Metric mặc định aggregate (privacy §6) — compliance (RPT-F-04)
/// là ngoại lệ theo dõi cá nhân vì đó là nghĩa vụ, không phải giám sát.
/// </summary>
public sealed class ReportService(QgpDbContext db) : IReportService
{
    private const int DueSoonDays = 30;

    public async Task<IssuanceReportDto> GetIssuanceAsync(CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var monthStart = new DateOnly(today.Year, today.Month, 1);

        var totalDocuments = await db.Documents.AsNoTracking().CountAsync(ct);
        var effectiveCount = await db.Documents.AsNoTracking().CountAsync(d => d.CurrentEffectiveVersionId != null, ct);

        var byType = await db.Documents.AsNoTracking()
            .GroupBy(d => d.Type)
            .Select(g => new { g.Key, C = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.C, ct);

        var byStatus = (await db.DocumentVersions.AsNoTracking()
                .GroupBy(v => v.Status)
                .Select(g => new { g.Key, C = g.Count() })
                .ToListAsync(ct))
            // Gộp UnderRevision (nội bộ) vào Draft khi phơi ra (Mapping.StatusToWire).
            .GroupBy(x => Mapping.StatusToWire(x.Key))
            .ToDictionary(g => g.Key, g => g.Sum(x => x.C));

        var issuedThisMonth = await db.DocumentVersions.AsNoTracking()
            .CountAsync(v => v.IssueDate != null && v.IssueDate >= monthStart, ct);

        var overdueReview = await db.Documents.AsNoTracking()
            .CountAsync(d => d.NextReviewDate != null && d.NextReviewDate < today, ct);
        var dueSoonReview = await db.Documents.AsNoTracking()
            .CountAsync(d => d.NextReviewDate != null && d.NextReviewDate >= today && d.NextReviewDate <= today.AddDays(DueSoonDays), ct);

        // RPT-F-02: tỷ lệ bản đã ban hành (Effective/Superseded) có change_summary (BR-04).
        var issued = await db.DocumentVersions.AsNoTracking()
            .Where(v => v.Status == VersionStatus.Effective || v.Status == VersionStatus.Superseded)
            .Select(v => v.ChangeSummary)
            .ToListAsync(ct);
        var withChangeSummaryPercent = issued.Count == 0
            ? 0
            : (int)Math.Round(100.0 * issued.Count(s => !string.IsNullOrWhiteSpace(s)) / issued.Count);

        return new IssuanceReportDto(
            totalDocuments, effectiveCount, byType, byStatus,
            issuedThisMonth, overdueReview, dueSoonReview, withChangeSummaryPercent);
    }

    public async Task<ComplianceReportDto> GetComplianceAsync(CancellationToken ct = default)
    {
        var docs = await db.Documents.AsNoTracking()
            .Where(d => d.MandatoryAck && d.CurrentEffectiveVersionId != null)
            .Select(d => new
            {
                d.DocId,
                d.Title,
                EffId = d.CurrentEffectiveVersionId!.Value,
                EffVersion = d.CurrentEffectiveVersion!.Version,
                RoleIds = d.AudienceRoles.Select(a => a.RoleId).ToList(),
            })
            .OrderBy(d => d.DocId)
            .ToListAsync(ct);

        if (docs.Count == 0)
            return new ComplianceReportDto(0, 0, []);

        // Batch load: audience users theo role, ack theo version, sso_subject.
        var allRoleIds = docs.SelectMany(d => d.RoleIds).Distinct().ToList();
        var roleUsers = await db.UserRoles.AsNoTracking()
            .Where(ur => allRoleIds.Contains(ur.RoleId))
            .Select(ur => new { ur.RoleId, ur.UserId })
            .ToListAsync(ct);

        var effIds = docs.Select(d => d.EffId).ToList();
        var acks = await db.Acknowledgements.AsNoTracking()
            .Where(a => effIds.Contains(a.VersionId))
            .Select(a => new { a.VersionId, a.UserId })
            .ToListAsync(ct);

        var audienceUserIds = roleUsers.Select(r => r.UserId).Distinct().ToList();
        var subMap = (await db.Users.AsNoTracking()
                .Where(u => audienceUserIds.Contains(u.Id))
                .Select(u => new { u.Id, u.SsoSubject })
                .ToListAsync(ct))
            .ToDictionary(x => x.Id, x => x.SsoSubject);

        var items = new List<ComplianceItemDto>();
        var fullyCompliant = 0;
        foreach (var d in docs)
        {
            var audience = roleUsers.Where(r => d.RoleIds.Contains(r.RoleId)).Select(r => r.UserId).ToHashSet();
            var acked = acks.Where(a => a.VersionId == d.EffId).Select(a => a.UserId).ToHashSet();
            var ackedInAudience = audience.Count(acked.Contains);
            var notRead = audience.Where(u => !acked.Contains(u))
                .Select(u => subMap.TryGetValue(u, out var s) ? s : u.ToString())
                .OrderBy(s => s, StringComparer.Ordinal)
                .ToList();
            var percent = audience.Count == 0 ? 0 : (int)Math.Round(100.0 * ackedInAudience / audience.Count);
            if (audience.Count > 0 && ackedInAudience == audience.Count) fullyCompliant++;

            items.Add(new ComplianceItemDto(d.DocId, d.Title, d.EffVersion, audience.Count, ackedInAudience, percent, notRead));
        }

        return new ComplianceReportDto(docs.Count, fullyCompliant, items);
    }

    public async Task<FeedbackReportDto> GetFeedbackAsync(CancellationToken ct = default)
    {
        var byStatus = (await db.Feedback.AsNoTracking()
                .GroupBy(f => f.Status)
                .Select(g => new { g.Key, C = g.Count() })
                .ToListAsync(ct))
            .ToDictionary(x => x.Key.ToString(), x => x.C);
        var total = byStatus.Values.Sum();

        // Thời gian xử lý TB: feedback đã chốt (Resolved/Rejected) — (updated_at - created_at).
        var resolved = await db.Feedback.AsNoTracking()
            .Where(f => f.Status == FeedbackStatus.Resolved || f.Status == FeedbackStatus.Rejected)
            .Select(f => new { f.CreatedAt, f.UpdatedAt })
            .ToListAsync(ct);
        double? avgHours = resolved.Count == 0
            ? null
            : Math.Round(resolved.Average(f => (f.UpdatedAt - f.CreatedAt).TotalHours), 1);

        return new FeedbackReportDto(total, byStatus, avgHours, resolved.Count);
    }

    public async Task<AccessReportDto> GetAccessAsync(CancellationToken ct = default)
    {
        // Proxy engagement theo lượt ack trên bản Effective (chưa có event lượt xem — RPT-F-03 đầy đủ ở P3).
        var docs = await db.Documents.AsNoTracking()
            .Where(d => d.CurrentEffectiveVersionId != null)
            .Select(d => new
            {
                d.DocId,
                d.Title,
                AckCount = db.Acknowledgements.Count(a => a.VersionId == d.CurrentEffectiveVersionId),
            })
            .ToListAsync(ct);

        var zeroAck = docs.Where(d => d.AckCount == 0)
            .OrderBy(d => d.DocId, StringComparer.Ordinal)
            .Take(20)
            .Select(d => new AccessItemDto(d.DocId, d.Title, 0))
            .ToList();
        var topEngaged = docs.Where(d => d.AckCount > 0)
            .OrderByDescending(d => d.AckCount).ThenBy(d => d.DocId, StringComparer.Ordinal)
            .Take(10)
            .Select(d => new AccessItemDto(d.DocId, d.Title, d.AckCount))
            .ToList();

        const string note = "Proxy theo lượt xác nhận đọc — CHƯA có dữ liệu lượt xem/tra cứu (cần event tracking, P3).";
        return new AccessReportDto(zeroAck.Count, zeroAck, topEngaged, note);
    }
}
