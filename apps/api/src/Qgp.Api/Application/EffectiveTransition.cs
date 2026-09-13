using Qgp.Api.Domain.Entities;
using Qgp.Api.Domain.Enums;

namespace Qgp.Api.Application;

/// <summary>
/// Chuyển 1 version sang Effective + supersede bản Effective cũ (BR-02) — dùng chung cho
/// publish-tới-hạn (VersionService) và scheduler WF-03 (EffectiveTransitionService).
/// Chốt chặn cuối là partial unique index uq_one_effective_per_doc.
/// </summary>
public static class EffectiveTransition
{
    /// <summary>Yêu cầu <paramref name="doc"/> đã nạp kèm Versions. Chỉ mutate entity, KHÔNG SaveChanges.</summary>
    public static void Apply(Document doc, DocumentVersion version, DateTimeOffset now)
    {
        if (version.Status == VersionStatus.Effective) return; // idempotent

        if (doc.CurrentEffectiveVersionId is { } oldId && oldId != version.Id)
        {
            var old = doc.Versions.FirstOrDefault(v => v.Id == oldId);
            if (old is not null)
            {
                old.Status = VersionStatus.Superseded;
                old.UpdatedAt = now;
            }
        }

        version.Status = VersionStatus.Effective;
        version.UpdatedAt = now;
        doc.CurrentEffectiveVersionId = version.Id;
    }
}
