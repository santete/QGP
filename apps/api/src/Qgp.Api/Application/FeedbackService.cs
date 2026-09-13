using Microsoft.EntityFrameworkCore;
using Qgp.Api.Contracts;
using Qgp.Api.Domain.Entities;
using Qgp.Api.Domain.Enums;
using Qgp.Api.Infrastructure.Persistence;

namespace Qgp.Api.Application;

public interface IFeedbackService
{
    Task<FeedbackDto> CreateAsync(CreateFeedbackRequest req, string? actorSub, CancellationToken ct = default);
    Task<IReadOnlyList<FeedbackDto>> ListAsync(string? status, string? category, string? docId, int limit, CancellationToken ct = default);
    Task<FeedbackDto> TriageAsync(Guid id, TriageFeedbackRequest req, string? actorSub, CancellationToken ct = default);
}

/// <summary>Feedback in-context (FBK-F-01/03/04): server tự gắn version/user, không tin client cho định danh.</summary>
public sealed class FeedbackService(QgpDbContext db, IAuditService audit) : IFeedbackService
{
    public async Task<FeedbackDto> CreateAsync(CreateFeedbackRequest req, string? actorSub, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(actorSub)) throw AppException.Validation("thiếu định danh người gửi");

        // Server resolve ngữ cảnh: doc_id + version → version_id (không tin client cho version_id).
        var version = await db.DocumentVersions
            .Include(v => v.Document)
            .FirstOrDefaultAsync(v => v.Document.DocId == req.DocId && v.Version == req.Version, ct)
            ?? throw AppException.NotFound("VERSION_NOT_FOUND", $"Không tìm thấy {req.DocId} v{req.Version}");

        var user = await db.Users.FirstOrDefaultAsync(u => u.SsoSubject == actorSub, ct);
        if (user is null)
        {
            user = new User { SsoSubject = actorSub };
            db.Users.Add(user);
            await db.SaveChangesAsync(ct);
        }

        var fb = new Feedback
        {
            VersionId = version.Id,
            UserId = user.Id,
            Category = Mapping.FeedbackCategoryFromWire(req.Category),
            Status = FeedbackStatus.New,
            Body = req.Body,
        };
        db.Feedback.Add(fb);
        await audit.LogAsync(actorSub, $"feedback.created {req.Category}", version.DocumentId, ct);
        await db.SaveChangesAsync(ct);

        return ToDto(fb, req.DocId, req.Version);
    }

    public async Task<IReadOnlyList<FeedbackDto>> ListAsync(string? status, string? category, string? docId, int limit, CancellationToken ct = default)
    {
        var q = db.Feedback.AsNoTracking().Include(f => f.Version).ThenInclude(v => v.Document).AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            q = q.Where(f => f.Status == Mapping.FeedbackStatusFromWire(status));
        if (!string.IsNullOrWhiteSpace(category))
            q = q.Where(f => f.Category == Mapping.FeedbackCategoryFromWire(category));
        if (!string.IsNullOrWhiteSpace(docId))
            q = q.Where(f => f.Version.Document.DocId == docId);

        var rows = await q.OrderByDescending(f => f.CreatedAt).Take(Math.Clamp(limit, 1, 200)).ToListAsync(ct);
        return rows.Select(f => ToDto(f, f.Version.Document.DocId, f.Version.Version)).ToList();
    }

    public async Task<FeedbackDto> TriageAsync(Guid id, TriageFeedbackRequest req, string? actorSub, CancellationToken ct = default)
    {
        var fb = await db.Feedback.Include(f => f.Version).ThenInclude(v => v.Document)
            .FirstOrDefaultAsync(f => f.Id == id, ct)
            ?? throw AppException.NotFound("FEEDBACK_NOT_FOUND", "Không tìm thấy feedback");

        fb.Status = Mapping.FeedbackStatusFromWire(req.Status);
        fb.UpdatedAt = DateTimeOffset.UtcNow;
        var noteSuffix = string.IsNullOrWhiteSpace(req.Note) ? "" : $" ({req.Note})";
        await audit.LogAsync(actorSub, $"feedback.triaged {req.Status}{noteSuffix}", fb.Version.DocumentId, ct);
        await db.SaveChangesAsync(ct);

        return ToDto(fb, fb.Version.Document.DocId, fb.Version.Version);
    }

    private static FeedbackDto ToDto(Feedback f, string docId, string version) => new(
        f.Id,
        f.VersionId,
        docId,
        version,
        Mapping.FeedbackCategoryToWire(f.Category),
        f.Status.ToString(),
        f.Body,
        f.CreatedAt);
}
