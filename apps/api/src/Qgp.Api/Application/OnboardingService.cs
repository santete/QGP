using Microsoft.EntityFrameworkCore;
using Qgp.Api.Contracts;
using Qgp.Api.Infrastructure.Persistence;

namespace Qgp.Api.Application;

public interface IOnboardingService
{
    /// <summary>
    /// Lộ trình onboarding gộp theo role của user (ONB-F-01/02/04, S12). Read-only.
    /// Mỗi item kèm cờ acked (đã xác nhận đọc bản Effective — DOC-F-09). progress = read/total.
    /// </summary>
    Task<OnboardingDto> GetForUserAsync(string? sub, IReadOnlyCollection<string> roleCodes, CancellationToken ct = default);
}

/// <summary>
/// Onboarding tự phục vụ (ONB): gộp learning_paths của mọi role user có → danh sách item xếp theo seq.
/// Trạng thái đọc lấy từ acknowledgements trên bản Effective hiện hành của tài liệu.
/// </summary>
public sealed class OnboardingService(QgpDbContext db) : IOnboardingService
{
    public async Task<OnboardingDto> GetForUserAsync(
        string? sub, IReadOnlyCollection<string> roleCodes, CancellationToken ct = default)
    {
        var roleIds = roleCodes.Count == 0
            ? []
            : await db.Roles.AsNoTracking()
                .Where(r => roleCodes.Contains(r.Code))
                .Select(r => r.Id)
                .ToListAsync(ct);

        if (roleIds.Count == 0)
            return new OnboardingDto(null, [], new OnboardingProgressDto(0, 0, 0), []);

        var paths = await db.LearningPaths.AsNoTracking()
            .Where(p => roleIds.Contains(p.RoleId))
            .OrderBy(p => p.Title)
            .Select(p => new { p.Title, p.RoleId })
            .ToListAsync(ct);

        // Item của các lộ trình khớp role, kèm bản Effective hiện hành của tài liệu (nếu có).
        var rows = await db.PathItems.AsNoTracking()
            .Where(i => roleIds.Contains(i.LearningPath.RoleId))
            .OrderBy(i => i.Seq).ThenBy(i => i.Document.DocId)
            .Select(i => new
            {
                i.Document.DocId,
                i.Document.Title,
                i.Seq,
                i.Mandatory,
                EffId = i.Document.CurrentEffectiveVersionId,
                EffVersion = i.Document.CurrentEffectiveVersion != null ? i.Document.CurrentEffectiveVersion.Version : null,
            })
            .ToListAsync(ct);

        // Gộp trùng tài liệu (nếu xuất hiện ở nhiều lộ trình theo nhiều role): giữ seq nhỏ nhất + mandatory nếu bất kỳ.
        var merged = rows
            .GroupBy(r => r.DocId)
            .Select(g => new
            {
                DocId = g.Key,
                g.First().Title,
                Seq = g.Min(x => x.Seq),
                Mandatory = g.Any(x => x.Mandatory),
                g.First().EffId,
                g.First().EffVersion,
            })
            .OrderBy(x => x.Seq).ThenBy(x => x.DocId, StringComparer.Ordinal)
            .ToList();

        // Ack của user trên bản Effective (read-only). Chưa có sub → chưa ack gì.
        var ackedEffIds = new HashSet<Guid>();
        if (!string.IsNullOrEmpty(sub))
        {
            var effIds = merged.Where(m => m.EffId is { }).Select(m => m.EffId!.Value).ToList();
            if (effIds.Count > 0)
                ackedEffIds = (await db.Acknowledgements.AsNoTracking()
                        .Where(a => a.User.SsoSubject == sub && effIds.Contains(a.VersionId))
                        .Select(a => a.VersionId)
                        .ToListAsync(ct))
                    .ToHashSet();
        }

        var items = merged.Select(m => new OnboardingItemDto(
            m.DocId,
            m.Title,
            m.Seq,
            m.Mandatory,
            m.EffVersion,
            m.EffId is { } id && ackedEffIds.Contains(id))).ToList();

        var total = items.Count;
        var read = items.Count(i => i.Acked);
        var percent = total == 0 ? 0 : (int)Math.Round(100.0 * read / total);

        var roleCodesInPaths = await db.Roles.AsNoTracking()
            .Where(r => paths.Select(p => p.RoleId).Contains(r.Id))
            .Select(r => r.Code)
            .ToListAsync(ct);

        var title = paths.Count == 1 ? paths[0].Title : null; // nhiều role → không có 1 tiêu đề duy nhất

        return new OnboardingDto(title, roleCodesInPaths, new OnboardingProgressDto(total, read, percent), items);
    }
}
