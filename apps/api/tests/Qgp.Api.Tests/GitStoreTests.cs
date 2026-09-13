using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Qgp.Api.Infrastructure.Git;
using Qgp.Api.Infrastructure.Persistence;

namespace Qgp.Api.Tests;

/// <summary>
/// Git content store (E1.6b, ADR-0015): publish → commit nội dung vào Git, set content_git_ref;
/// đọc lại từ Git đúng nội dung. Cần Postgres (docker compose up).
/// </summary>
public class GitStoreTests(QgpApiFactory factory) : IClassFixture<QgpApiFactory>
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

    [Fact]
    public async Task Publish_commits_content_to_git_and_sets_content_git_ref()
    {
        var client = await LoginAsync("AUTHOR", "APPROVER");
        var docId = $"IT-GIT-{Guid.NewGuid():N}"[..18];
        var today = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");
        const string content = "# Nội dung ban hành\n\nDòng quy trình.";

        try
        {
            (await client.PostAsJsonAsync("/v1/documents", new
            {
                doc_id = docId,
                title = "Tài liệu git",
                type = "Process",
                content_markdown = content,
            })).EnsureSuccessStatusCode();

            var versions = await client.GetFromJsonAsync<JsonElement>($"/v1/documents/{docId}/versions");
            var vid = versions.EnumerateArray().Single().GetProperty("id").GetString();

            (await client.PostAsync($"/v1/versions/{vid}/submit", null)).EnsureSuccessStatusCode();
            (await client.PostAsJsonAsync($"/v1/versions/{vid}/approve", new { decision = "approve" })).EnsureSuccessStatusCode();
            (await client.PostAsJsonAsync($"/v1/versions/{vid}/publish", new
            {
                issue_date = today,
                effective_date = today,
                change_summary = "Ban hành lần đầu",
            })).EnsureSuccessStatusCode();

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<QgpDbContext>();
            var git = scope.ServiceProvider.GetRequiredService<IGitContentStore>();

            var ver = await db.DocumentVersions
                .Include(v => v.Document)
                .FirstAsync(v => v.Document.DocId == docId && v.Version == "1.0");

            // content_git_ref = SHA-1 commit (40 hex).
            Assert.False(string.IsNullOrWhiteSpace(ver.ContentGitRef));
            Assert.Equal(40, ver.ContentGitRef!.Length);

            // Đọc lại nội dung từ Git theo SHA → đúng bản đã publish.
            var fromGit = git.ReadVersion(docId, "1.0", ver.ContentGitRef);
            Assert.Equal(content, fromGit);
        }
        finally
        {
            await CleanupAsync(docId);
        }
    }

    private async Task CleanupAsync(string docId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<QgpDbContext>();
        var doc = db.Documents.FirstOrDefault(d => d.DocId == docId);
        if (doc is null) return;
        var logs = db.AuditLogs.Where(a => a.DocumentId == doc.Id);
        db.AuditLogs.RemoveRange(logs);
        doc.CurrentEffectiveVersionId = null;
        await db.SaveChangesAsync();
        db.Documents.Remove(doc);
        await db.SaveChangesAsync();
    }
}
