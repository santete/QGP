using Microsoft.EntityFrameworkCore;
using Qgp.Api.Domain.Entities;
using Qgp.Api.Infrastructure.Persistence;

namespace Qgp.Api.Application;

public interface IUserProvisioningService
{
    /// <summary>
    /// CHIẾU (project) danh tính từ token OIDC vào DB (B0): upsert <c>users</c> theo sso_subject
    /// + đồng bộ <c>user_roles</c> khớp đúng bộ role trong token (mirror: thêm role mới, gỡ role cũ).
    /// Keycloak vẫn là nguồn phân quyền (authz đọc claim) — DB chỉ chiếu để B1 (xem user/role) +
    /// B2 (gửi thông báo cho QA_LEAD/ADMIN) có dữ liệu. Role code không có trong bảng roles → bỏ qua.
    /// </summary>
    Task SyncAsync(string sub, string? displayName, string? email, IReadOnlyCollection<string> roleCodes, CancellationToken ct = default);
}

public sealed class UserProvisioningService(QgpDbContext db) : IUserProvisioningService
{
    public async Task SyncAsync(string sub, string? displayName, string? email, IReadOnlyCollection<string> roleCodes, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(sub)) return;

        // Upsert user (race-safe qua ON CONFLICT — nhiều tab login cùng lúc không vỡ unique).
        // Chỉ cập nhật display_name/email khi token có (COALESCE) → không xoá dữ liệu đã có bằng null.
        await db.Database.ExecuteSqlInterpolatedAsync(
            $@"INSERT INTO qgp.users (id, sso_subject, display_name, email, created_at, updated_at)
               VALUES (gen_random_uuid(), {sub}, {displayName}, {email}, now(), now())
               ON CONFLICT (sso_subject) DO UPDATE SET
                   display_name = COALESCE(EXCLUDED.display_name, qgp.users.display_name),
                   email        = COALESCE(EXCLUDED.email, qgp.users.email),
                   updated_at   = now()", ct);

        var userId = await db.Users.Where(u => u.SsoSubject == sub).Select(u => u.Id).FirstAsync(ct);

        // Map role code (token) → role id (bảng roles seed §6.2). Bỏ code lạ (default-deny giống ClaimsTransformation).
        var wanted = await db.Roles.AsNoTracking()
            .Where(r => roleCodes.Contains(r.Code))
            .Select(r => r.Id)
            .ToListAsync(ct);
        var wantedSet = wanted.ToHashSet();

        var current = await db.UserRoles.Where(ur => ur.UserId == userId).ToListAsync(ct);
        var currentSet = current.Select(ur => ur.RoleId).ToHashSet();

        var toRemove = current.Where(ur => !wantedSet.Contains(ur.RoleId)).ToList();
        var toAdd = wanted.Where(id => !currentSet.Contains(id))
            .Select(id => new UserRole { UserId = userId, RoleId = id });

        if (toRemove.Count > 0) db.UserRoles.RemoveRange(toRemove);
        db.UserRoles.AddRange(toAdd);

        await db.SaveChangesAsync(ct);
    }
}
