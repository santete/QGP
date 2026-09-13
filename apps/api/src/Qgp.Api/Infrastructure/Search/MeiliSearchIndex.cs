using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Qgp.Api.Infrastructure.Search;

/// <summary>Document lưu trên Meilisearch (index "documents"). Chỉ bản Effective (BR-06).</summary>
public sealed class SearchDoc
{
    [JsonPropertyName("id")] public string Id { get; set; } = null!;      // = doc_id (primary key)
    [JsonPropertyName("doc_id")] public string DocId { get; set; } = null!;
    [JsonPropertyName("version")] public string Version { get; set; } = null!;
    [JsonPropertyName("title")] public string Title { get; set; } = null!;
    [JsonPropertyName("content")] public string Content { get; set; } = "";
    [JsonPropertyName("type")] public string Type { get; set; } = "";
    [JsonPropertyName("tags")] public string[] Tags { get; set; } = [];
    [JsonPropertyName("effective_date")] public string? EffectiveDate { get; set; }
    [JsonPropertyName("_rankingScore")] public double? RankingScore { get; set; }
}

public interface ISearchIndex
{
    Task EnsureConfiguredAsync(CancellationToken ct = default);
    Task UpsertAsync(SearchDoc doc, CancellationToken ct = default);
    Task DeleteAsync(string id, CancellationToken ct = default);
    Task ReplaceAllAsync(IReadOnlyList<SearchDoc> docs, CancellationToken ct = default);
    Task<IReadOnlyList<SearchDoc>> SearchAsync(string? q, string? type, string? tag, int limit, CancellationToken ct = default);
}

/// <summary>
/// Client Meilisearch REST thô. HttpClient preconfig (BaseAddress + Authorization + Timeout) ở DI
/// → key gửi trên MỌI request kể cả GET /tasks (SDK chính thức bỏ sót path này → 401).
/// Chờ task hoàn tất để đảm bảo searchable ngay (test/deterministic).
/// </summary>
public sealed class MeiliSearchIndex(HttpClient http) : ISearchIndex
{
    public const string IndexName = "documents";
    private const string Base = $"/indexes/{IndexName}";
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task EnsureConfiguredAsync(CancellationToken ct = default)
    {
        await WaitAsync(await PutTaskAsync($"{Base}/settings/searchable-attributes", new[] { "title", "content", "tags" }, ct), ct);
        await WaitAsync(await PutTaskAsync($"{Base}/settings/filterable-attributes", new[] { "type", "tags" }, ct), ct);
    }

    public async Task UpsertAsync(SearchDoc doc, CancellationToken ct = default)
    {
        var res = await http.PostAsJsonAsync($"{Base}/documents?primaryKey=id", new[] { doc }, Json, ct);
        await WaitAsync(await TaskUidAsync(res, ct), ct);
    }

    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        var res = await http.DeleteAsync($"{Base}/documents/{Uri.EscapeDataString(id)}", ct);
        await WaitAsync(await TaskUidAsync(res, ct), ct);
    }

    public async Task ReplaceAllAsync(IReadOnlyList<SearchDoc> docs, CancellationToken ct = default)
    {
        var clear = await http.DeleteAsync($"{Base}/documents", ct);
        await WaitAsync(await TaskUidAsync(clear, ct), ct);
        if (docs.Count == 0) return;
        var res = await http.PostAsJsonAsync($"{Base}/documents?primaryKey=id", docs, Json, ct);
        await WaitAsync(await TaskUidAsync(res, ct), ct);
    }

    public async Task<IReadOnlyList<SearchDoc>> SearchAsync(string? q, string? type, string? tag, int limit, CancellationToken ct = default)
    {
        var filters = new List<string>();
        if (!string.IsNullOrWhiteSpace(type)) filters.Add($"type = \"{type}\"");
        if (!string.IsNullOrWhiteSpace(tag)) filters.Add($"tags = \"{tag}\"");

        var body = new Dictionary<string, object?>
        {
            ["q"] = q ?? "",
            ["limit"] = limit,
            ["showRankingScore"] = true,
            ["filter"] = filters.Count > 0 ? string.Join(" AND ", filters) : null,
        };

        var res = await http.PostAsJsonAsync($"{Base}/search", body, Json, ct);
        res.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync(ct));
        if (!doc.RootElement.TryGetProperty("hits", out var hits)) return [];
        return hits.EnumerateArray()
            .Select(h => h.Deserialize<SearchDoc>())
            .Where(x => x is not null)
            .Select(x => x!)
            .ToList();
    }

    // ---- helpers ----------------------------------------------------
    private async Task<int> PutTaskAsync(string path, object body, CancellationToken ct)
    {
        var res = await http.PutAsJsonAsync(path, body, Json, ct);
        return await TaskUidAsync(res, ct);
    }

    private static async Task<int> TaskUidAsync(HttpResponseMessage res, CancellationToken ct)
    {
        res.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync(ct));
        return doc.RootElement.GetProperty("taskUid").GetInt32();
    }

    /// <summary>Poll GET /tasks/{uid} tới khi succeeded (throw nếu failed / quá hạn).</summary>
    private async Task WaitAsync(int taskUid, CancellationToken ct)
    {
        for (var i = 0; i < 50; i++)
        {
            var res = await http.GetAsync($"/tasks/{taskUid}", ct);
            res.EnsureSuccessStatusCode();
            using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync(ct));
            var status = doc.RootElement.GetProperty("status").GetString();
            if (status == "succeeded") return;
            if (status == "failed" || status == "canceled")
                throw new InvalidOperationException($"Meili task {taskUid} {status}");
            await Task.Delay(100, ct);
        }
        throw new TimeoutException($"Meili task {taskUid} chưa xong sau 5s");
    }
}
