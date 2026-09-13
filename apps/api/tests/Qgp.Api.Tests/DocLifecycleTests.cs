using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Qgp.Api.Infrastructure.Persistence;

namespace Qgp.Api.Tests;

/// <summary>
/// Integration test WF-01 (create→submit→approve→publish→đọc Effective) + RBAC.
/// Cần Postgres (docker compose up). Mỗi test dùng doc_id ngẫu nhiên + dọn dẹp cuối.
/// </summary>
public class DocLifecycleTests(QgpApiFactory factory) : IClassFixture<QgpApiFactory>
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
    public async Task Full_lifecycle_create_submit_approve_publish_then_effective()
    {
        var client = await LoginAsync("AUTHOR", "APPROVER");
        var docId = $"IT-TEST-{Guid.NewGuid():N}"[..20];
        var today = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");

        try
        {
            var create = await client.PostAsJsonAsync("/v1/documents", new
            {
                doc_id = docId,
                title = "Tài liệu kiểm thử",
                type = "Process",
                mandatory_ack = true,
                content_markdown = "# Xin chào\n\n1. Bước một\n2. Bước hai\n3. Bước ba",
            });
            Assert.Equal(HttpStatusCode.Created, create.StatusCode);

            // Lấy version v1.0 vừa tạo.
            var versions = await client.GetFromJsonAsync<JsonElement>($"/v1/documents/{docId}/versions");
            var v1 = versions.EnumerateArray().Single(v => v.GetProperty("version").GetString() == "1.0");
            var vid = v1.GetProperty("id").GetString();

            (await client.PostAsync($"/v1/versions/{vid}/submit", null)).EnsureSuccessStatusCode();
            var approve = await client.PostAsJsonAsync($"/v1/versions/{vid}/approve", new { decision = "approve" });
            approve.EnsureSuccessStatusCode();
            var publish = await client.PostAsJsonAsync($"/v1/versions/{vid}/publish", new
            {
                issue_date = today,
                effective_date = today,
                change_summary = "Ban hành lần đầu",
            });
            publish.EnsureSuccessStatusCode();

            var detail = await client.GetFromJsonAsync<JsonElement>($"/v1/documents/{docId}");
            Assert.True(detail.GetProperty("is_effective").GetBoolean());
            Assert.Equal("Effective", detail.GetProperty("status").GetString());
            Assert.Equal("1.0", detail.GetProperty("effective_version").GetString());
            Assert.Contains("Xin ch", detail.GetProperty("content_html").GetString());
            Assert.Contains("<ol>", detail.GetProperty("content_html").GetString()); // markdown → html
        }
        finally
        {
            await CleanupAsync(docId);
        }
    }

    [Fact]
    public async Task Reader_cannot_create_document_403()
    {
        var client = await LoginAsync("READER");
        var res = await client.PostAsJsonAsync("/v1/documents", new { doc_id = "X", title = "y", type = "Process" });
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Get_unknown_document_returns_404_with_error_code()
    {
        var client = await LoginAsync("READER");
        var res = await client.GetAsync("/v1/documents/NO-SUCH-DOC-999");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("DOCUMENT_NOT_FOUND", body.GetProperty("error").GetProperty("code").GetString());
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
