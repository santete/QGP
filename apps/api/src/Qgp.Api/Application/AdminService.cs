using Microsoft.EntityFrameworkCore;
using Qgp.Api.Auth;
using Qgp.Api.Contracts;
using Qgp.Api.Domain.Entities;
using Qgp.Api.Infrastructure.Persistence;

namespace Qgp.Api.Application;

public interface IAdminService
{
    /// <summary>Ma trận RBAC §10.1 (CHỈ ĐỌC) — policy → role. Nguồn authz thật là token Keycloak.</summary>
    Task<RbacMatrixDto> GetRbacAsync(CancellationToken ct = default);

    /// <summary>Danh sách user (lớp chiếu B0) + role. ADM-F-01. Sửa role làm ở Keycloak.</summary>
    Task<IReadOnlyList<AdminUserDto>> ListUsersAsync(int limit, CancellationToken ct = default);

    // Taxonomy / tag (ADM-F-02).
    Task<IReadOnlyList<TagDto>> ListTagsAsync(CancellationToken ct = default);
    Task<TagDto> CreateTagAsync(CreateTagRequest req, CancellationToken ct = default);
    Task<TagDto> UpdateTagAsync(Guid id, UpdateTagRequest req, CancellationToken ct = default);
    Task DeleteTagAsync(Guid id, CancellationToken ct = default);

    // Loại tài liệu (ADM-F-02, doc-types — B1 chiều sâu).
    Task<IReadOnlyList<DocTypeDto>> ListDocTypesAsync(bool activeOnly, CancellationToken ct = default);
    Task<DocTypeDto> CreateDocTypeAsync(CreateDocTypeRequest req, CancellationToken ct = default);
    Task<DocTypeDto> UpdateDocTypeAsync(Guid id, UpdateDocTypeRequest req, CancellationToken ct = default);
    Task DeleteDocTypeAsync(Guid id, CancellationToken ct = default);
}

/// <summary>Quản trị hệ thống S17 (ADM-F-01 RBAC read · ADM-F-02 taxonomy). RBAC admin.config.</summary>
public sealed class AdminService(QgpDbContext db) : IAdminService
{
    // Thứ tự hiển thị policy (theo luồng quyền tăng dần) — Dictionary không đảm bảo thứ tự.
    private static readonly string[] PolicyOrder =
        [QgpPolicies.DocRead, QgpPolicies.KbContribute, QgpPolicies.DocAuthor, QgpPolicies.DocApprove, QgpPolicies.AdminConfig];

    public async Task<RbacMatrixDto> GetRbacAsync(CancellationToken ct = default)
    {
        var roleNames = await db.Roles.AsNoTracking().ToDictionaryAsync(r => r.Code, r => r.Name, ct);
        // Sắp role theo thứ tự chuẩn §10.1 (READER..ADMIN); role lạ (nếu có) xếp cuối.
        var roles = QgpRoles.All
            .Where(roleNames.ContainsKey)
            .Select(code => new RoleDto(code, roleNames[code]))
            .ToList();

        var policies = PolicyOrder
            .Where(QgpPolicies.RolesFor.ContainsKey)
            .Select(p => new RbacPolicyDto(p, OrderRoles(QgpPolicies.RolesFor[p])))
            .ToList();

        return new RbacMatrixDto(roles, policies);
    }

    private static IReadOnlyList<string> OrderRoles(IEnumerable<string> codes)
    {
        var set = codes.ToHashSet();
        return QgpRoles.All.Where(set.Contains).ToList();
    }

    public async Task<IReadOnlyList<AdminUserDto>> ListUsersAsync(int limit, CancellationToken ct = default)
    {
        return await db.Users.AsNoTracking()
            .OrderBy(u => u.SsoSubject)
            .Take(Math.Clamp(limit, 1, 500))
            .Select(u => new AdminUserDto(
                u.SsoSubject,
                u.DisplayName,
                u.Email,
                u.UserRoles.Select(ur => ur.Role.Code).OrderBy(c => c).ToList()))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<TagDto>> ListTagsAsync(CancellationToken ct = default)
    {
        return await db.Tags.AsNoTracking()
            .OrderBy(t => t.Slug)
            .Select(t => new TagDto(t.Id, t.Slug, t.Name, t.DocTags.Count))
            .ToListAsync(ct);
    }

    public async Task<TagDto> CreateTagAsync(CreateTagRequest req, CancellationToken ct = default)
    {
        var slug = (req.Slug ?? "").Trim();
        var name = (req.Name ?? "").Trim();
        if (string.IsNullOrWhiteSpace(slug)) throw AppException.Validation("slug bắt buộc");
        if (string.IsNullOrWhiteSpace(name)) name = slug;
        if (await db.Tags.AnyAsync(t => t.Slug == slug, ct))
            throw AppException.Conflict("TAG_SLUG_CONFLICT", $"slug '{slug}' đã tồn tại");

        var tag = new Tag { Slug = slug, Name = name };
        db.Tags.Add(tag);
        await db.SaveChangesAsync(ct);
        return new TagDto(tag.Id, tag.Slug, tag.Name, 0);
    }

    public async Task<TagDto> UpdateTagAsync(Guid id, UpdateTagRequest req, CancellationToken ct = default)
    {
        var tag = await db.Tags.FirstOrDefaultAsync(t => t.Id == id, ct)
            ?? throw AppException.NotFound("TAG_NOT_FOUND", "Không tìm thấy tag");

        if (!string.IsNullOrWhiteSpace(req.Slug))
        {
            var slug = req.Slug.Trim();
            if (slug != tag.Slug && await db.Tags.AnyAsync(t => t.Slug == slug, ct))
                throw AppException.Conflict("TAG_SLUG_CONFLICT", $"slug '{slug}' đã tồn tại");
            tag.Slug = slug;
        }
        if (!string.IsNullOrWhiteSpace(req.Name)) tag.Name = req.Name.Trim();
        tag.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
        var docCount = await db.DocTags.CountAsync(dt => dt.TagId == id, ct);
        return new TagDto(tag.Id, tag.Slug, tag.Name, docCount);
    }

    public async Task DeleteTagAsync(Guid id, CancellationToken ct = default)
    {
        var tag = await db.Tags.FirstOrDefaultAsync(t => t.Id == id, ct)
            ?? throw AppException.NotFound("TAG_NOT_FOUND", "Không tìm thấy tag");

        if (await db.DocTags.AnyAsync(dt => dt.TagId == id, ct))
            throw AppException.Conflict("TAG_IN_USE", "Tag đang được gắn cho tài liệu — không thể xoá");

        db.Tags.Remove(tag);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<DocTypeDto>> ListDocTypesAsync(bool activeOnly, CancellationToken ct = default)
    {
        var q = db.DocTypes.AsNoTracking().AsQueryable();
        if (activeOnly) q = q.Where(t => t.Active);
        return await q
            .OrderBy(t => t.Seq).ThenBy(t => t.Code)
            .Select(t => new DocTypeDto(t.Id, t.Code, t.Label, t.Seq, t.Active,
                db.Documents.Count(d => d.Type == t.Code)))
            .ToListAsync(ct);
    }

    public async Task<DocTypeDto> CreateDocTypeAsync(CreateDocTypeRequest req, CancellationToken ct = default)
    {
        var code = (req.Code ?? "").Trim();
        if (string.IsNullOrWhiteSpace(code)) throw AppException.Validation("code bắt buộc");
        if (code.Length > 50) throw AppException.Validation("code tối đa 50 ký tự");
        if (await db.DocTypes.AnyAsync(t => t.Code == code, ct))
            throw AppException.Conflict("DOC_TYPE_CONFLICT", $"loại tài liệu '{code}' đã tồn tại");

        var label = string.IsNullOrWhiteSpace(req.Label) ? code : req.Label.Trim();
        var seq = req.Seq ?? ((await db.DocTypes.MaxAsync(t => (int?)t.Seq, ct) ?? 0) + 1);
        var dt = new DocType { Code = code, Label = label, Seq = seq, Active = true };
        db.DocTypes.Add(dt);
        await db.SaveChangesAsync(ct);
        return new DocTypeDto(dt.Id, dt.Code, dt.Label, dt.Seq, dt.Active, 0);
    }

    public async Task<DocTypeDto> UpdateDocTypeAsync(Guid id, UpdateDocTypeRequest req, CancellationToken ct = default)
    {
        var dt = await db.DocTypes.FirstOrDefaultAsync(t => t.Id == id, ct)
            ?? throw AppException.NotFound("DOC_TYPE_NOT_FOUND", "Không tìm thấy loại tài liệu");

        // Code là bất biến (đang dùng ở documents.type) — chỉ sửa label/seq/active.
        if (!string.IsNullOrWhiteSpace(req.Label)) dt.Label = req.Label.Trim();
        if (req.Seq is { } s) dt.Seq = s;
        if (req.Active is { } a) dt.Active = a;
        dt.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        var docCount = await db.Documents.CountAsync(d => d.Type == dt.Code, ct);
        return new DocTypeDto(dt.Id, dt.Code, dt.Label, dt.Seq, dt.Active, docCount);
    }

    public async Task DeleteDocTypeAsync(Guid id, CancellationToken ct = default)
    {
        var dt = await db.DocTypes.FirstOrDefaultAsync(t => t.Id == id, ct)
            ?? throw AppException.NotFound("DOC_TYPE_NOT_FOUND", "Không tìm thấy loại tài liệu");

        if (await db.Documents.AnyAsync(d => d.Type == dt.Code, ct))
            throw AppException.Conflict("DOC_TYPE_IN_USE", "Loại tài liệu đang được dùng — không thể xoá");

        db.DocTypes.Remove(dt);
        await db.SaveChangesAsync(ct);
    }
}
