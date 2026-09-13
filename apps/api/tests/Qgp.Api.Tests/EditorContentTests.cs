using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Qgp.Api.Infrastructure.Persistence;

namespace Qgp.Api.Tests;

/// <summary>
/// Integration test editor S7 (GET/PATCH /v1/versions/{id}/content): sửa được Draft,
/// bất biến sau khi ban hành (BR-03), RBAC doc.author. Cần Postgres (docker compose up).
/// </summary>
public class EditorContentTests(QgpApiFactory factory) : IClassFixture<QgpApiFactory>
{
    private readonly QgpApiFactory _factory = factory;

    private async Task<HttpClient> LoginAsync(params string[] roles)
    {
        var client = _factory.CreateClient();
        var res = await client.PostAsJsonAsync("/auth/dev-login", new { sub = "tester@fpt", roles });
        res.EnsureSuccessStatusCode();
        var token = (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("access_token").GetString();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static async Task<string> FirstVersionIdAsync(HttpClient client, string docId)
    {
        var versions = await client.GetFromJsonAsync<JsonElement>($"/v1/documents/{docId}/versions");
        return versions.EnumerateArray().Single().GetProperty("id").GetString()!;
    }

    [Fact]
    public async Task Author_can_get_and_patch_draft_content()
    {
        var client = await LoginAsync("AUTHOR");
        var docId = $"IT-EDT-{Guid.NewGuid():N}"[..18];

        try
        {
            (await client.PostAsJsonAsync("/v1/documents", new
            {
                doc_id = docId,
                title = "Tài liệu soạn thảo",
                type = "Process",
                content_markdown = "# Bản nháp đầu",
            })).EnsureSuccessStatusCode();

            var vid = await FirstVersionIdAsync(client, docId);

            // GET content → đúng markdown ban đầu.
            var got = await client.GetFromJsonAsync<JsonElement>($"/v1/versions/{vid}/content");
            Assert.Equal("# Bản nháp đầu", got.GetProperty("content_markdown").GetString());
            Assert.Equal("Draft", got.GetProperty("status").GetString());

            // PATCH content → cập nhật.
            var patch = await client.PatchAsJsonAsync($"/v1/versions/{vid}/content",
                new { content_markdown = "# Bản nháp sửa\n\nNội dung mới." });
            patch.EnsureSuccessStatusCode();
            var updated = await patch.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("# Bản nháp sửa\n\nNội dung mới.", updated.GetProperty("content_markdown").GetString());

            // GET lại xác nhận đã lưu.
            var reread = await client.GetFromJsonAsync<JsonElement>($"/v1/versions/{vid}/content");
            Assert.Contains("Bản nháp sửa", reread.GetProperty("content_markdown").GetString());
        }
        finally
        {
            await CleanupAsync(docId);
        }
    }

    [Fact]
    public async Task Cannot_patch_effective_version_immutable_409()
    {
        var client = await LoginAsync("AUTHOR", "APPROVER");
        var docId = $"IT-EDT-{Guid.NewGuid():N}"[..18];
        var today = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");

        try
        {
            (await client.PostAsJsonAsync("/v1/documents", new
            {
                doc_id = docId,
                title = "Tài liệu ban hành",
                type = "Process",
                content_markdown = "# Nội dung",
            })).EnsureSuccessStatusCode();

            var vid = await FirstVersionIdAsync(client, docId);
            (await client.PostAsync($"/v1/versions/{vid}/submit", null)).EnsureSuccessStatusCode();
            (await client.PostAsJsonAsync($"/v1/versions/{vid}/approve", new { decision = "approve" })).EnsureSuccessStatusCode();
            (await client.PostAsJsonAsync($"/v1/versions/{vid}/publish", new
            {
                issue_date = today,
                effective_date = today,
                change_summary = "Ban hành",
            })).EnsureSuccessStatusCode();

            // Đã Effective → PATCH content phải 409 VERSION_IMMUTABLE (BR-03).
            var patch = await client.PatchAsJsonAsync($"/v1/versions/{vid}/content",
                new { content_markdown = "# Sửa sau ban hành" });
            Assert.Equal(HttpStatusCode.Conflict, patch.StatusCode);
            var body = await patch.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("VERSION_IMMUTABLE", body.GetProperty("error").GetProperty("code").GetString());
        }
        finally
        {
            await CleanupAsync(docId);
        }
    }

    [Fact]
    public async Task Reader_cannot_patch_content_403()
    {
        var reader = await LoginAsync("READER");
        var res = await reader.PatchAsJsonAsync($"/v1/versions/{Guid.NewGuid()}/content",
            new { content_markdown = "x" });
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    private async Task CleanupAsync(string docId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<QgpDbContext>();
        var doc = db.Documents.FirstOrDefault(d => d.DocId == docId);
        if (doc is null) return;
        doc.CurrentEffectiveVersionId = null;
        await db.SaveChangesAsync();
        db.Documents.Remove(doc);
        await db.SaveChangesAsync();
    }
}
