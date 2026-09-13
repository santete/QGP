using Markdig;
using Microsoft.EntityFrameworkCore;
using Qgp.Api.Contracts;
using Qgp.Api.Infrastructure.Persistence;
using Qgp.Api.Infrastructure.Search;

namespace Qgp.Api.Application;

public interface ISearchService
{
    /// <summary>Đồng bộ index cho 1 tài liệu: upsert bản Effective, hoặc xoá nếu không còn Effective (BR-06). Best-effort.</summary>
    Task IndexEffectiveAsync(string docId, CancellationToken ct = default);
    Task<int> ReindexAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<SearchHitDto>> SearchAsync(string? q, string? type, string? tag, int limit, CancellationToken ct = default);
}

/// <summary>Tra cứu full-text qua Meilisearch — chỉ index tài liệu Effective (BR-06, NFR-02).</summary>
public sealed class SearchService(QgpDbContext db, ISearchIndex index, ILogger<SearchService> logger) : ISearchService
{
    public async Task IndexEffectiveAsync(string docId, CancellationToken ct = default)
    {
        try
        {
            var doc = await db.Documents
                .Include(d => d.Versions)
                .Include(d => d.DocTags).ThenInclude(t => t.Tag)
                .FirstOrDefaultAsync(d => d.DocId == docId, ct);

            var eff = doc?.CurrentEffectiveVersionId is { } id
                ? doc.Versions.FirstOrDefault(v => v.Id == id)
                : null;

            if (doc is null || eff is null)
            {
                if (doc is not null) await index.DeleteAsync(doc.DocId, ct); // hết Effective → gỡ khỏi index
                return;
            }

            await index.UpsertAsync(Build(doc, eff), ct);
        }
        catch (Exception ex)
        {
            // Best-effort: Meili lỗi/không sẵn sàng KHÔNG được làm hỏng nghiệp vụ; /admin/reindex sửa drift.
            logger.LogWarning(ex, "Index Effective thất bại cho {DocId}", docId);
        }
    }

    public async Task<int> ReindexAllAsync(CancellationToken ct = default)
    {
        await index.EnsureConfiguredAsync(ct);

        var docs = await db.Documents
            .Where(d => d.CurrentEffectiveVersionId != null)
            .Include(d => d.Versions)
            .Include(d => d.DocTags).ThenInclude(t => t.Tag)
            .ToListAsync(ct);

        var payload = docs
            .Select(d => (d, eff: d.Versions.FirstOrDefault(v => v.Id == d.CurrentEffectiveVersionId)))
            .Where(x => x.eff is not null)
            .Select(x => Build(x.d, x.eff!))
            .ToList();

        await index.ReplaceAllAsync(payload, ct);
        return payload.Count;
    }

    public async Task<IReadOnlyList<SearchHitDto>> SearchAsync(string? q, string? type, string? tag, int limit, CancellationToken ct = default)
    {
        var hits = await index.SearchAsync(q, type, tag, limit, ct);
        return hits.Select(h => new SearchHitDto(
            h.DocId,
            h.Version,
            h.Title,
            Snippet(h.Content),
            h.RankingScore ?? 0)).ToList();
    }

    private static SearchDoc Build(Domain.Entities.Document doc, Domain.Entities.DocumentVersion eff) => new()
    {
        Id = doc.DocId,
        DocId = doc.DocId,
        Version = eff.Version,
        Title = doc.Title,
        Content = Markdown.ToPlainText(eff.ContentMarkdown ?? ""),
        Type = doc.Type,
        Tags = doc.DocTags.Select(t => t.Tag.Slug).ToArray(),
        EffectiveDate = eff.EffectiveDate?.ToString("yyyy-MM-dd"),
    };

    private static string Snippet(string content)
    {
        content = content.Replace('\n', ' ').Trim();
        return content.Length <= 160 ? content : content[..160] + "…";
    }
}
