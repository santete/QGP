using Microsoft.EntityFrameworkCore;
using Qgp.Api.Contracts;
using Qgp.Api.Domain.Entities;
using Qgp.Api.Domain.Enums;
using Qgp.Api.Infrastructure.Persistence;

namespace Qgp.Api.Application;

public interface IDocumentService
{
    Task<DocumentDetailDto> GetAsync(string docId, CancellationToken ct = default);
    Task<IReadOnlyList<DocumentSummaryDto>> ListAsync(string? q, string? type, string? tag, int limit, CancellationToken ct = default);
    Task<IReadOnlyList<DocumentVersionDto>> ListVersionsAsync(string docId, CancellationToken ct = default);
    Task<IReadOnlyList<ReviewQueueItemDto>> ListReviewQueueAsync(VersionStatus status, int limit, CancellationToken ct = default);
    Task<DiffResultDto> GetDiffAsync(string docId, string from, string to, CancellationToken ct = default);
    Task<DocumentDetailDto> CreateAsync(CreateDocumentRequest req, string? actorSub, CancellationToken ct = default);

    /// <summary>Audience roles của tài liệu (ADM-F-02, S17) — nguồn cho REC/notification.</summary>
    Task<IReadOnlyList<DocAudienceItemDto>> GetAudienceAsync(string docId, CancellationToken ct = default);

    /// <summary>Đặt lại toàn bộ audience roles của tài liệu (thay thế tập cũ). Validate role code.</summary>
    Task<IReadOnlyList<DocAudienceItemDto>> SetAudienceAsync(string docId, SetDocAudienceRequest req, string? actorSub, CancellationToken ct = default);
}

public sealed class DocumentService(QgpDbContext db, IAuditService audit) : IDocumentService
{
    public async Task<DocumentDetailDto> GetAsync(string docId, CancellationToken ct = default)
    {
        var doc = await LoadAsync(docId, ct)
            ?? throw AppException.NotFound("DOCUMENT_NOT_FOUND", $"Không tìm thấy tài liệu {docId}");

        var eff = doc.CurrentEffectiveVersionId is { } id
            ? doc.Versions.FirstOrDefault(v => v.Id == id)
            : null;
        var view = eff ?? doc.Versions.OrderByDescending(v => v.CreatedAt).FirstOrDefault();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var upcoming = doc.Versions
            .Where(v => v.Status == VersionStatus.Published && v.EffectiveDate is { } d && d > today)
            .OrderBy(v => v.EffectiveDate)
            .FirstOrDefault();
        var badge = upcoming?.EffectiveDate is { } ed ? $"Sắp áp dụng từ {ed:dd/MM/yyyy}" : null;

        return new DocumentDetailDto(
            DocId: doc.DocId,
            Title: doc.Title,
            Type: doc.Type,
            Classification: doc.Classification,
            EffectiveVersion: eff?.Version,
            Status: eff is not null ? "Effective" : Mapping.StatusToWire(view?.Status ?? VersionStatus.Draft),
            Tags: doc.DocTags.Select(t => t.Tag.Slug).OrderBy(s => s).ToList(),
            MandatoryAck: doc.MandatoryAck,
            NextReviewDate: doc.NextReviewDate,
            AudienceRoles: doc.AudienceRoles.Select(a => a.Role.Code).OrderBy(s => s).ToList(),
            IsEffective: eff is not null,
            Badge: badge,
            EffectiveLink: null,
            ContentHtml: Mapping.RenderHtml(view?.ContentMarkdown));
    }

    public async Task<IReadOnlyList<DocumentSummaryDto>> ListAsync(string? q, string? type, string? tag, int limit, CancellationToken ct = default)
    {
        var query = db.Documents.AsNoTracking()
            .Include(d => d.Versions)
            .Include(d => d.DocTags).ThenInclude(t => t.Tag)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(d => EF.Functions.ILike(d.Title, $"%{q}%") || EF.Functions.ILike(d.DocId, $"%{q}%"));
        if (!string.IsNullOrWhiteSpace(type))
            query = query.Where(d => d.Type == type);
        if (!string.IsNullOrWhiteSpace(tag))
            query = query.Where(d => d.DocTags.Any(t => t.Tag.Slug == tag));

        var docs = await query.OrderBy(d => d.DocId).Take(Math.Clamp(limit, 1, 200)).ToListAsync(ct);

        return docs.Select(doc =>
        {
            var eff = doc.CurrentEffectiveVersionId is { } id ? doc.Versions.FirstOrDefault(v => v.Id == id) : null;
            var view = eff ?? doc.Versions.OrderByDescending(v => v.CreatedAt).FirstOrDefault();
            return new DocumentSummaryDto(
                doc.DocId,
                doc.Title,
                doc.Type,
                doc.Classification,
                eff?.Version,
                eff is not null ? "Effective" : Mapping.StatusToWire(view?.Status ?? Domain.Enums.VersionStatus.Draft),
                doc.DocTags.Select(t => t.Tag.Slug).OrderBy(s => s).ToList());
        }).ToList();
    }

    public async Task<IReadOnlyList<DocumentVersionDto>> ListVersionsAsync(string docId, CancellationToken ct = default)
    {
        var doc = await db.Documents.AsNoTracking()
            .Include(d => d.Versions)
            .FirstOrDefaultAsync(d => d.DocId == docId, ct)
            ?? throw AppException.NotFound("DOCUMENT_NOT_FOUND", $"Không tìm thấy tài liệu {docId}");

        return doc.Versions
            .OrderByDescending(v => v.CreatedAt)
            .Select(v => Mapping.ToVersionDto(v, docId))
            .ToList();
    }

    public async Task<IReadOnlyList<ReviewQueueItemDto>> ListReviewQueueAsync(VersionStatus status, int limit, CancellationToken ct = default)
    {
        // Hàng đợi theo trạng thái: InReview=chờ duyệt (S9), Approved=chờ ban hành (S8). FIFO theo updated_at.
        return await db.DocumentVersions.AsNoTracking()
            .Where(v => v.Status == status)
            .OrderBy(v => v.UpdatedAt)
            .Take(Math.Clamp(limit, 1, 200))
            .Select(v => new ReviewQueueItemDto(v.Id, v.Document.DocId, v.Document.Title, v.Version, v.UpdatedAt))
            .ToListAsync(ct);
    }

    public async Task<DiffResultDto> GetDiffAsync(string docId, string from, string to, CancellationToken ct = default)
    {
        var doc = await db.Documents.AsNoTracking().Include(d => d.Versions)
            .FirstOrDefaultAsync(d => d.DocId == docId, ct)
            ?? throw AppException.NotFound("DOCUMENT_NOT_FOUND", $"Không tìm thấy tài liệu {docId}");

        var vFrom = doc.Versions.FirstOrDefault(v => v.Version == from)
            ?? throw AppException.Validation($"Không tìm thấy version {from}");
        var vTo = doc.Versions.FirstOrDefault(v => v.Version == to)
            ?? throw AppException.Validation($"Không tìm thấy version {to}");

        var diff = UnifiedDiff.Compute(vFrom.ContentMarkdown, vTo.ContentMarkdown, $"{docId}@{from}", $"{docId}@{to}");
        return new DiffResultDto(from, to, diff, vTo.ChangeSummary);
    }

    public async Task<DocumentDetailDto> CreateAsync(CreateDocumentRequest req, string? actorSub, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(req.DocId)) throw AppException.Validation("doc_id bắt buộc");
        if (string.IsNullOrWhiteSpace(req.Title)) throw AppException.Validation("title bắt buộc");

        if (await db.Documents.AnyAsync(d => d.DocId == req.DocId, ct))
            throw AppException.Conflict("DOC_ID_CONFLICT", $"doc_id {req.DocId} đã tồn tại (BR-01)");

        // Validate loại tài liệu theo bảng doc_types active (B1 chiều sâu — thay enum cứng).
        if (string.IsNullOrWhiteSpace(req.Type) || !await db.DocTypes.AnyAsync(t => t.Code == req.Type && t.Active, ct))
            throw AppException.Validation($"type không hợp lệ: {req.Type}");

        var doc = new Document
        {
            Id = Guid.NewGuid(), // gán client-side để audit document.created ghi đúng document_id (không phải Guid.Empty)
            DocId = req.DocId,
            Title = req.Title,
            Type = req.Type,
            Classification = req.Classification,
            MandatoryAck = req.MandatoryAck ?? false,
            NextReviewDate = req.NextReviewDate,
        };

        // v1.0 Draft (UC-01) — nội dung tạm ở DB.
        doc.Versions.Add(new DocumentVersion
        {
            Version = "1.0",
            Status = VersionStatus.Draft,
            ContentMarkdown = req.ContentMarkdown,
        });

        foreach (var slug in (req.Tags ?? []).Distinct())
            doc.DocTags.Add(new DocTag { Tag = await GetOrCreateTagAsync(slug, ct) });

        foreach (var code in (req.AudienceRoles ?? []).Distinct())
        {
            var role = await db.Roles.FirstOrDefaultAsync(r => r.Code == code, ct)
                ?? throw AppException.Validation($"role không hợp lệ: {code}");
            doc.AudienceRoles.Add(new DocAudienceRole { Role = role });
        }

        db.Documents.Add(doc);
        await audit.LogAsync(actorSub, "document.created", doc.Id, ct);
        await db.SaveChangesAsync(ct);

        return await GetAsync(req.DocId, ct);
    }

    public async Task<IReadOnlyList<DocAudienceItemDto>> GetAudienceAsync(string docId, CancellationToken ct = default)
    {
        var doc = await db.Documents.AsNoTracking()
            .Include(d => d.AudienceRoles).ThenInclude(a => a.Role)
            .FirstOrDefaultAsync(d => d.DocId == docId, ct)
            ?? throw AppException.NotFound("DOCUMENT_NOT_FOUND", $"Không tìm thấy tài liệu {docId}");

        return doc.AudienceRoles
            .OrderBy(a => a.Role.Code)
            .Select(a => new DocAudienceItemDto(a.Role.Code, a.Reason))
            .ToList();
    }

    public async Task<IReadOnlyList<DocAudienceItemDto>> SetAudienceAsync(string docId, SetDocAudienceRequest req, string? actorSub, CancellationToken ct = default)
    {
        var doc = await db.Documents
            .Include(d => d.AudienceRoles)
            .FirstOrDefaultAsync(d => d.DocId == docId, ct)
            ?? throw AppException.NotFound("DOCUMENT_NOT_FOUND", $"Không tìm thấy tài liệu {docId}");

        // Chuẩn hoá yêu cầu: dedupe theo role code (reason cuối thắng), validate role tồn tại.
        var roleMap = await db.Roles.ToDictionaryAsync(r => r.Code, r => r, ct);
        var wanted = new Dictionary<string, string?>();
        foreach (var a in req.Audience ?? [])
        {
            if (string.IsNullOrWhiteSpace(a.Role)) continue;
            if (!roleMap.ContainsKey(a.Role)) throw AppException.Validation($"role không hợp lệ: {a.Role}");
            wanted[a.Role] = string.IsNullOrWhiteSpace(a.Reason) ? null : a.Reason.Trim();
        }
        var wantedRoleIds = wanted.Keys.Select(c => roleMap[c].Id).ToHashSet();

        // Reconcile (tránh xoá+chèn cùng PK): gỡ role thừa, cập nhật reason role giữ, thêm role mới.
        var existing = doc.AudienceRoles.ToDictionary(a => a.RoleId);
        foreach (var stale in doc.AudienceRoles.Where(a => !wantedRoleIds.Contains(a.RoleId)).ToList())
            db.DocAudienceRoles.Remove(stale);
        foreach (var (code, reason) in wanted)
        {
            var roleId = roleMap[code].Id;
            if (existing.TryGetValue(roleId, out var ar)) ar.Reason = reason;
            else db.DocAudienceRoles.Add(new DocAudienceRole { DocumentId = doc.Id, RoleId = roleId, Reason = reason });
        }

        await audit.LogAsync(actorSub, "document.audience_updated", doc.Id, ct);
        await db.SaveChangesAsync(ct);

        return wanted.OrderBy(kv => kv.Key).Select(kv => new DocAudienceItemDto(kv.Key, kv.Value)).ToList();
    }

    private async Task<Tag> GetOrCreateTagAsync(string slug, CancellationToken ct)
    {
        var existing = await db.Tags.FirstOrDefaultAsync(t => t.Slug == slug, ct);
        if (existing is not null) return existing;
        var tag = new Tag { Slug = slug, Name = slug };
        db.Tags.Add(tag);
        return tag;
    }

    private Task<Document?> LoadAsync(string docId, CancellationToken ct) =>
        db.Documents
            .Include(d => d.Versions)
            .Include(d => d.DocTags).ThenInclude(t => t.Tag)
            .Include(d => d.AudienceRoles).ThenInclude(a => a.Role)
            .FirstOrDefaultAsync(d => d.DocId == docId, ct);
}
