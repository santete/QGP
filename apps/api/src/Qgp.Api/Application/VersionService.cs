using Microsoft.EntityFrameworkCore;
using Qgp.Api.Contracts;
using Qgp.Api.Domain.Entities;
using Qgp.Api.Domain.Enums;
using Qgp.Api.Infrastructure.Git;
using Qgp.Api.Infrastructure.Persistence;

namespace Qgp.Api.Application;

public interface IVersionService
{
    Task<DocumentVersionDto> CreateVersionAsync(string docId, CreateVersionRequest req, string? actorSub, CancellationToken ct = default);
    Task<VersionContentDto> GetContentAsync(Guid versionId, CancellationToken ct = default);
    Task<VersionContentDto> UpdateContentAsync(Guid versionId, UpdateContentRequest req, string? actorSub, CancellationToken ct = default);
    Task<DocumentVersionDto> SubmitAsync(Guid versionId, string? actorSub, CancellationToken ct = default);
    Task<DocumentVersionDto> ApproveAsync(Guid versionId, ApproveRequest req, string? actorSub, CancellationToken ct = default);
    Task<PublishResultDto> PublishAsync(Guid versionId, PublishRequest req, string? actorSub, CancellationToken ct = default);
    Task<AcknowledgementDto> AcknowledgeAsync(Guid versionId, string actorSub, CancellationToken ct = default);
}

/// <summary>Vòng đời tài liệu WF-01 (state machine SDD §4.3) + acknowledge (DOC-F-09).</summary>
public sealed class VersionService(QgpDbContext db, IAuditService audit, ISearchService search, IGitContentStore git, INotificationService notifications) : IVersionService
{
    public async Task<DocumentVersionDto> CreateVersionAsync(string docId, CreateVersionRequest req, string? actorSub, CancellationToken ct = default)
    {
        if (req.ChangeType is not ("minor" or "major"))
            throw AppException.Validation("change_type phải là 'minor' hoặc 'major' (BR-05)");

        var doc = await db.Documents.Include(d => d.Versions).FirstOrDefaultAsync(d => d.DocId == docId, ct)
            ?? throw AppException.NotFound("DOCUMENT_NOT_FOUND", $"Không tìm thấy tài liệu {docId}");

        var latest = doc.Versions.OrderByDescending(v => ParseVersion(v.Version)).First();
        var (major, minor) = ParseVersion(latest.Version);
        var next = req.ChangeType == "major" ? $"{major + 1}.0" : $"{major}.{minor + 1}";

        var v = new DocumentVersion
        {
            DocumentId = doc.Id,
            Version = next,
            Status = VersionStatus.Draft,
            ContentMarkdown = req.ContentMarkdown,
        };
        db.DocumentVersions.Add(v);
        await audit.LogAsync(actorSub, $"version.created {next} ({req.ChangeType})", doc.Id);
        await db.SaveChangesAsync(ct);
        return Mapping.ToVersionDto(v, docId);
    }

    public async Task<VersionContentDto> GetContentAsync(Guid versionId, CancellationToken ct = default)
    {
        var v = await LoadVersionAsync(versionId, ct);
        return new VersionContentDto(v.Id, v.Document.DocId, v.Version, Mapping.StatusToWire(v.Status), v.ContentMarkdown ?? "");
    }

    public async Task<VersionContentDto> UpdateContentAsync(Guid versionId, UpdateContentRequest req, string? actorSub, CancellationToken ct = default)
    {
        var v = await LoadVersionAsync(versionId, ct);
        // Chỉ sửa được bản chưa ban hành (BR-03: Published/Effective/Superseded/Retired bất biến).
        if (v.Status is not (VersionStatus.Draft or VersionStatus.UnderRevision))
            throw AppException.Conflict("VERSION_IMMUTABLE", $"Chỉ sửa nội dung bản Draft/UnderRevision (hiện: {v.Status}) — BR-03");

        v.ContentMarkdown = req.ContentMarkdown;
        v.UpdatedAt = DateTimeOffset.UtcNow;
        await audit.LogAsync(actorSub, $"version.content_updated {v.Version}", v.DocumentId);
        await db.SaveChangesAsync(ct);
        return new VersionContentDto(v.Id, v.Document.DocId, v.Version, Mapping.StatusToWire(v.Status), v.ContentMarkdown ?? "");
    }

    public async Task<DocumentVersionDto> SubmitAsync(Guid versionId, string? actorSub, CancellationToken ct = default)
    {
        var v = await LoadVersionAsync(versionId, ct);
        if (v.Status is not (VersionStatus.Draft or VersionStatus.UnderRevision))
            throw AppException.StateConflict($"Chỉ submit được bản Draft (hiện: {v.Status})");

        v.Status = VersionStatus.InReview;
        v.UpdatedAt = DateTimeOffset.UtcNow;
        await audit.LogAsync(actorSub, $"version.submitted {v.Version}", v.DocumentId);
        await db.SaveChangesAsync(ct);
        return Mapping.ToVersionDto(v, v.Document.DocId);
    }

    public async Task<DocumentVersionDto> ApproveAsync(Guid versionId, ApproveRequest req, string? actorSub, CancellationToken ct = default)
    {
        var v = await LoadVersionAsync(versionId, ct);
        if (v.Status != VersionStatus.InReview)
            throw AppException.StateConflict($"Chỉ duyệt được bản InReview (hiện: {v.Status})");

        switch (req.Decision)
        {
            case "approve":
                v.Status = VersionStatus.Approved;
                break;
            case "reject":
                if (string.IsNullOrWhiteSpace(req.Comment))
                    throw AppException.Validation("reject bắt buộc kèm comment");
                v.Status = VersionStatus.Draft;
                break;
            default:
                throw AppException.Validation("decision phải là 'approve' hoặc 'reject'");
        }

        v.UpdatedAt = DateTimeOffset.UtcNow;
        await audit.LogAsync(actorSub, $"version.{req.Decision} {v.Version}", v.DocumentId);
        await db.SaveChangesAsync(ct);
        return Mapping.ToVersionDto(v, v.Document.DocId);
    }

    public async Task<PublishResultDto> PublishAsync(Guid versionId, PublishRequest req, string? actorSub, CancellationToken ct = default)
    {
        var v = await LoadVersionAsync(versionId, ct);

        if (v.Status is VersionStatus.Published or VersionStatus.Effective or VersionStatus.Superseded)
            throw AppException.Conflict("VERSION_IMMUTABLE", "Bản đã ban hành là bất biến (BR-03)");
        if (v.Status != VersionStatus.Approved)
            throw AppException.StateConflict($"Chỉ publish được bản Approved (hiện: {v.Status})");
        if (string.IsNullOrWhiteSpace(req.ChangeSummary))
            throw AppException.Validation("change_summary bắt buộc (BR-04)");
        if (req.EffectiveDate < req.IssueDate)
            throw AppException.Validation("effective_date phải >= issue_date (BR-07)");

        v.IssueDate = req.IssueDate;
        v.EffectiveDate = req.EffectiveDate;
        v.ChangeSummary = req.ChangeSummary;
        v.Status = VersionStatus.Published;
        v.UpdatedAt = DateTimeOffset.UtcNow;

        // Docs-as-code (ADR-0001): commit nội dung vào Git tại thời điểm publish → bất biến (BR-03).
        v.ContentGitRef = git.CommitVersion(
            v.Document.DocId, v.Version, v.ContentMarkdown ?? string.Empty,
            actorSub ?? "system",
            $"publish {v.Document.DocId} v{v.Version}: {req.ChangeSummary}");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        string? badge = null;
        if (req.EffectiveDate <= today)
        {
            // Tới hạn → Effective ngay + supersede bản cũ (BR-02). Bản tương lai do scheduler WF-03 chuyển.
            EffectiveTransition.Apply(v.Document, v, DateTimeOffset.UtcNow);
            // Thông báo audience (ADM-F-04, S19) — commit chung cùng transaction.
            await notifications.CreateForAudienceAsync(v.DocumentId, v.Document.DocId, v.Version, ct);
        }
        else
        {
            badge = $"Sắp áp dụng từ {req.EffectiveDate:dd/MM/yyyy}";
        }

        await audit.LogAsync(actorSub, $"version.published {v.Version} eff={req.EffectiveDate:yyyy-MM-dd}", v.DocumentId);
        await db.SaveChangesAsync(ct);

        if (v.Status == VersionStatus.Effective)
            await search.IndexEffectiveAsync(v.Document.DocId, ct); // chỉ index Effective (BR-06)

        return new PublishResultDto(v.Id, v.Document.DocId, v.Version, Mapping.StatusToWire(v.Status), v.EffectiveDate, badge);
    }

    public async Task<AcknowledgementDto> AcknowledgeAsync(Guid versionId, string actorSub, CancellationToken ct = default)
    {
        var v = await db.DocumentVersions.FirstOrDefaultAsync(x => x.Id == versionId, ct)
            ?? throw AppException.NotFound("VERSION_NOT_FOUND", "Không tìm thấy version");

        var user = await db.Users.FirstOrDefaultAsync(u => u.SsoSubject == actorSub, ct);
        if (user is null)
        {
            user = new User { SsoSubject = actorSub };
            db.Users.Add(user);
            await db.SaveChangesAsync(ct); // cần user.Id cho ack
        }

        var existing = await db.Acknowledgements.FirstOrDefaultAsync(a => a.UserId == user.Id && a.VersionId == versionId, ct);
        if (existing is not null)
            return new AcknowledgementDto(existing.Id, existing.VersionId, existing.AckedAt);

        var ack = new Acknowledgement { UserId = user.Id, VersionId = versionId, AckedAt = DateTimeOffset.UtcNow };
        db.Acknowledgements.Add(ack);
        await audit.LogAsync(actorSub, $"version.acknowledged", v.DocumentId);
        await db.SaveChangesAsync(ct);
        return new AcknowledgementDto(ack.Id, ack.VersionId, ack.AckedAt);
    }

    private async Task<DocumentVersion> LoadVersionAsync(Guid id, CancellationToken ct) =>
        await db.DocumentVersions
            .Include(v => v.Document).ThenInclude(d => d.Versions)
            .FirstOrDefaultAsync(v => v.Id == id, ct)
        ?? throw AppException.NotFound("VERSION_NOT_FOUND", "Không tìm thấy version");

    private static (int major, int minor) ParseVersion(string v)
    {
        var parts = v.Split('.');
        return parts.Length == 2 && int.TryParse(parts[0], out var ma) && int.TryParse(parts[1], out var mi)
            ? (ma, mi)
            : (1, 0);
    }
}
