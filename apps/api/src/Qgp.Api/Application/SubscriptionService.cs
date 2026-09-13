using Microsoft.EntityFrameworkCore;
using Qgp.Api.Contracts;
using Qgp.Api.Domain.Entities;
using Qgp.Api.Infrastructure.Persistence;

namespace Qgp.Api.Application;

public interface ISubscriptionService
{
    Task<IReadOnlyList<SubscriptionDto>> ListAsync(string? sub, CancellationToken ct = default);
    Task<SubscriptionStatusDto> SubscribeAsync(string docId, string? sub, CancellationToken ct = default);
    Task<SubscriptionStatusDto> UnsubscribeAsync(string docId, string? sub, CancellationToken ct = default);
}

/// <summary>Theo dõi tài liệu (ADM-F-04, S19). User được thông báo khi tài liệu theo dõi có bản Effective mới.</summary>
public sealed class SubscriptionService(QgpDbContext db) : ISubscriptionService
{
    public async Task<IReadOnlyList<SubscriptionDto>> ListAsync(string? sub, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(sub)) return [];
        return await db.Subscriptions.AsNoTracking()
            .Where(s => s.User.SsoSubject == sub)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new SubscriptionDto(s.Document.DocId, s.Document.Title, s.CreatedAt))
            .ToListAsync(ct);
    }

    public async Task<SubscriptionStatusDto> SubscribeAsync(string docId, string? sub, CancellationToken ct = default)
    {
        var (userId, documentId) = await ResolveAsync(docId, sub, ct);

        if (!await db.Subscriptions.AnyAsync(s => s.UserId == userId && s.DocumentId == documentId, ct))
        {
            db.Subscriptions.Add(new Subscription { UserId = userId, DocumentId = documentId });
            await db.SaveChangesAsync(ct);
        }
        return new SubscriptionStatusDto(docId, true);
    }

    public async Task<SubscriptionStatusDto> UnsubscribeAsync(string docId, string? sub, CancellationToken ct = default)
    {
        var (userId, documentId) = await ResolveAsync(docId, sub, ct);
        await db.Subscriptions
            .Where(s => s.UserId == userId && s.DocumentId == documentId)
            .ExecuteDeleteAsync(ct);
        return new SubscriptionStatusDto(docId, false);
    }

    /// <summary>Lấy user_id (upsert theo sso_subject) + document_id (theo doc_id business key).</summary>
    private async Task<(Guid userId, Guid documentId)> ResolveAsync(string docId, string? sub, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(sub)) throw AppException.Validation("Thiếu định danh người dùng");

        var documentId = await db.Documents.Where(d => d.DocId == docId).Select(d => d.Id).FirstOrDefaultAsync(ct);
        if (documentId == Guid.Empty)
            throw AppException.NotFound("DOCUMENT_NOT_FOUND", $"Không tìm thấy tài liệu {docId}");

        // Upsert user race-safe (giống AuditService) — theo dõi là first-action có thể xảy ra trước audit.
        await db.Database.ExecuteSqlInterpolatedAsync(
            $@"INSERT INTO qgp.users (id, sso_subject, created_at, updated_at)
               VALUES (gen_random_uuid(), {sub}, now(), now())
               ON CONFLICT (sso_subject) DO NOTHING", ct);
        var userId = await db.Users.Where(u => u.SsoSubject == sub).Select(u => u.Id).FirstAsync(ct);
        return (userId, documentId);
    }
}
