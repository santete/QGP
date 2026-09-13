using Microsoft.EntityFrameworkCore;
using Qgp.Api.Contracts;
using Qgp.Api.Domain.Enums;
using Qgp.Api.Infrastructure.Persistence;

namespace Qgp.Api.Application;

public interface IRecommendationService
{
    /// <summary>
    /// Gợi ý tài liệu theo role (BR-11, SDD §5.2). Read-only (KHÔNG upsert user).
    /// </summary>
    /// <param name="sub">sso_subject của user (null → không tra ack).</param>
    /// <param name="roleCodes">Mã role từ claim JWT (READER/AUTHOR/…).</param>
    Task<RecommendationsDto> GetForUserAsync(string? sub, IReadOnlyCollection<string> roleCodes, CancellationToken ct = default);
}

/// <summary>
/// REC rules-based explainable (BR-11). Không ML ở P2 → mọi gợi ý có <c>reason</c> (SDD §5.2).
/// candidates = Effective ∩ readable(doc.read: mọi role) ∩ audience_roles(user.roles).
/// </summary>
public sealed class RecommendationService(QgpDbContext db) : IRecommendationService
{
    private const int RecentDays = 14; // "vừa cập nhật" nếu effective_date trong 14 ngày (SDD §5.2)

    // Reason explainable — hiển thị nguyên văn ở UI (BR-11).
    private const string ReasonMandatory = "Bắt buộc";
    private const string ReasonReRead = "Cần đọc lại";
    private const string ReasonRoleMatch = "Khớp vai trò";
    private const string ReasonRecentlyUpdated = "Vừa cập nhật";

    public async Task<RecommendationsDto> GetForUserAsync(
        string? sub, IReadOnlyCollection<string> roleCodes, CancellationToken ct = default)
    {
        // Role của user → role_id. Không role nào hợp lệ → không có candidate.
        var roleIds = roleCodes.Count == 0
            ? []
            : await db.Roles.AsNoTracking()
                .Where(r => roleCodes.Contains(r.Code))
                .Select(r => r.Id)
                .ToListAsync(ct);

        if (roleIds.Count == 0)
            return new RecommendationsDto([], []);

        // Candidate: tài liệu có bản Effective + audience_roles giao với role của user.
        var candidates = await db.Documents.AsNoTracking()
            .Where(d => d.CurrentEffectiveVersionId != null
                && d.AudienceRoles.Any(ar => roleIds.Contains(ar.RoleId)))
            .Select(d => new
            {
                d.DocId,
                d.Title,
                d.MandatoryAck,
                EffId = d.CurrentEffectiveVersionId!.Value,
                EffVersion = d.CurrentEffectiveVersion!.Version,
                EffDate = d.CurrentEffectiveVersion!.EffectiveDate,
            })
            .ToListAsync(ct);

        if (candidates.Count == 0)
            return new RecommendationsDto([], []);

        // Ack của user (read-only): bản Effective đã ack + doc có ack ở BẤT KỲ version (để phát hiện "cần đọc lại").
        var ackedEffIds = new HashSet<Guid>();
        var docsWithPriorAck = new HashSet<string>();
        if (!string.IsNullOrEmpty(sub))
        {
            var effIds = candidates.Select(c => c.EffId).ToList();
            ackedEffIds = (await db.Acknowledgements.AsNoTracking()
                    .Where(a => a.User.SsoSubject == sub && effIds.Contains(a.VersionId))
                    .Select(a => a.VersionId)
                    .ToListAsync(ct))
                .ToHashSet();

            docsWithPriorAck = (await db.Acknowledgements.AsNoTracking()
                    .Where(a => a.User.SsoSubject == sub)
                    .Select(a => a.Version.Document.DocId)
                    .Distinct()
                    .ToListAsync(ct))
                .ToHashSet();
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var mustRead = new List<RecommendationItemDto>();
        var suggested = new List<RecommendationItemDto>();

        foreach (var c in candidates)
        {
            var acked = ackedEffIds.Contains(c.EffId);
            var isMajor = c.EffVersion.EndsWith(".0", StringComparison.Ordinal); // BR-05: major = x.0
            var recentlyUpdated = c.EffDate is { } ed && ed >= today.AddDays(-RecentDays);

            string reason;
            List<RecommendationItemDto> bucket;

            if (c.MandatoryAck && !acked)
            {
                reason = ReasonMandatory;                 // bắt buộc, chưa xác nhận
                bucket = mustRead;
            }
            else if (!acked && isMajor && docsWithPriorAck.Contains(c.DocId))
            {
                reason = ReasonReRead;                    // đã đọc bản cũ, có major mới
                bucket = mustRead;
            }
            else
            {
                reason = recentlyUpdated ? ReasonRecentlyUpdated : ReasonRoleMatch;
                bucket = suggested;
            }

            bucket.Add(new RecommendationItemDto(c.DocId, c.EffVersion, c.Title, c.EffDate, reason));
        }

        // Trong bucket: ưu tiên (bắt buộc, mới cập nhật) — effective_date desc, tie-break theo doc_id (ổn định).
        static IReadOnlyList<RecommendationItemDto> Sort(IEnumerable<RecommendationItemDto> items) =>
            items
                .OrderByDescending(i => i.EffectiveDate ?? DateOnly.MinValue)
                .ThenBy(i => i.DocId, StringComparer.Ordinal)
                .ToList();

        return new RecommendationsDto(Sort(mustRead), Sort(suggested));
    }
}
