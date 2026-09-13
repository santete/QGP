using Microsoft.EntityFrameworkCore;
using Qgp.Api.Contracts;
using Qgp.Api.Domain.Entities;
using Qgp.Api.Infrastructure.Persistence;

namespace Qgp.Api.Application;

public interface IAuditService
{
    /// <summary>
    /// Ghi 1 dòng audit (bất biến, ADM-F-03/NFR-03). KHÔNG gọi SaveChanges — caller commit chung.
    /// Resolve actor_id từ sso_subject (upsert User nếu chưa có) — actor ở cột actor_id, KHÔNG nhét vào action.
    /// </summary>
    Task LogAsync(string? actorSub, string action, Guid? documentId, CancellationToken ct = default);

    /// <summary>Truy vết audit log (ADM-F-03, S16) — resolve actor_id → sso_subject; lọc doc/thời gian; mới nhất trước.</summary>
    Task<IReadOnlyList<AuditEntryDto>> ListAsync(Guid? documentId, DateTimeOffset? from, DateTimeOffset? to, int limit, CancellationToken ct = default);
}

public sealed class AuditService(QgpDbContext db) : IAuditService
{
    public async Task LogAsync(string? actorSub, string action, Guid? documentId, CancellationToken ct = default)
    {
        Guid? actorId = null;
        if (!string.IsNullOrEmpty(actorSub))
            actorId = await ResolveActorIdAsync(actorSub, ct);

        db.AuditLogs.Add(new AuditLog
        {
            ActorId = actorId,
            Action = action,
            DocumentId = documentId,
            At = DateTimeOffset.UtcNow,
        });
    }

    public async Task<IReadOnlyList<AuditEntryDto>> ListAsync(Guid? documentId, DateTimeOffset? from, DateTimeOffset? to, int limit, CancellationToken ct = default)
    {
        var q = db.AuditLogs.AsNoTracking().AsQueryable();
        if (documentId is { } d) q = q.Where(a => a.DocumentId == d);
        if (from is { } f) q = q.Where(a => a.At >= f);   // At = partition key → pruning
        if (to is { } t) q = q.Where(a => a.At <= t);

        return await q
            .OrderByDescending(a => a.At)
            .Take(Math.Clamp(limit, 1, 200))
            .Select(a => new AuditEntryDto(
                a.Id,
                a.ActorId == null
                    ? "system"
                    : db.Users.Where(u => u.Id == a.ActorId).Select(u => u.SsoSubject).FirstOrDefault(),
                a.Action,
                a.DocumentId,
                a.At))
            .ToListAsync(ct);
    }

    /// <summary>
    /// Lấy user_id theo sso_subject, tạo nếu chưa có. Dùng INSERT … ON CONFLICT (race-safe:
    /// nhiều request cùng first-action của 1 user không vỡ unique uq_users_sso_subject).
    /// Ưu tiên user đã track trong request để khỏi round-trip thừa.
    /// </summary>
    private async Task<Guid> ResolveActorIdAsync(string sub, CancellationToken ct)
    {
        var tracked = db.Users.Local.FirstOrDefault(u => u.SsoSubject == sub);
        if (tracked is not null) return tracked.Id;

        // INSERT trực tiếp (ExecuteSql, KHÔNG compose) + ON CONFLICT DO NOTHING → race-safe,
        // không đụng pending changes của caller. Rồi SELECT id (chắc chắn tồn tại sau upsert).
        await db.Database.ExecuteSqlInterpolatedAsync(
            $@"INSERT INTO qgp.users (id, sso_subject, created_at, updated_at)
               VALUES (gen_random_uuid(), {sub}, now(), now())
               ON CONFLICT (sso_subject) DO NOTHING", ct);
        return await db.Users.Where(u => u.SsoSubject == sub).Select(u => u.Id).FirstAsync(ct);
    }
}
