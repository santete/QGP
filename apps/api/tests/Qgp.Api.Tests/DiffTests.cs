using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Qgp.Api.Infrastructure.Persistence;

namespace Qgp.Api.Tests;

/// <summary>
/// Integration test diff 2 version (S6, DOC-F-06): GET /v1/documents/{docId}/diff?from&to.
/// Cần Postgres (docker compose up).
/// </summary>
public class DiffTests(QgpApiFactory factory) : IClassFixture<QgpApiFactory>
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
    public async Task Diff_between_two_versions_shows_added_and_removed_lines()
    {
        var client = await LoginAsync("AUTHOR");
        var docId = $"IT-DIF-{Guid.NewGuid():N}"[..18];

        try
        {
            (await client.PostAsJsonAsync("/v1/documents", new
            {
                doc_id = docId,
                title = "Tài liệu diff",
                type = "Process",
                content_markdown = "# Tiêu đề\n\nDòng một\nDòng hai",
            })).EnsureSuccessStatusCode();

            // Tạo version 1.1 (minor) với nội dung khác.
            (await client.PostAsJsonAsync($"/v1/documents/{docId}/versions", new
            {
                change_type = "minor",
                content_markdown = "# Tiêu đề\n\nDòng một sửa\nDòng hai\nDòng ba",
            })).EnsureSuccessStatusCode();

            var diff = await client.GetFromJsonAsync<JsonElement>($"/v1/documents/{docId}/diff?from=1.0&to=1.1");
            var text = diff.GetProperty("diff").GetString()!;

            Assert.Equal("1.0", diff.GetProperty("from").GetString());
            Assert.Equal("1.1", diff.GetProperty("to").GetString());
            Assert.Contains("-Dòng một", text);          // dòng cũ bị xoá
            Assert.Contains("+Dòng một sửa", text);       // dòng mới thêm
            Assert.Contains("+Dòng ba", text);            // dòng thêm cuối
            Assert.Contains("@@", text);                  // có hunk header unified
        }
        finally
        {
            await CleanupAsync(docId);
        }
    }

    [Fact]
    public async Task Diff_unknown_version_returns_422()
    {
        var client = await LoginAsync("AUTHOR");
        var docId = $"IT-DIF-{Guid.NewGuid():N}"[..18];
        try
        {
            (await client.PostAsJsonAsync("/v1/documents", new
            {
                doc_id = docId,
                title = "x",
                type = "Process",
                content_markdown = "# a",
            })).EnsureSuccessStatusCode();

            var res = await client.GetAsync($"/v1/documents/{docId}/diff?from=1.0&to=9.9");
            Assert.Equal(HttpStatusCode.UnprocessableEntity, res.StatusCode);
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
