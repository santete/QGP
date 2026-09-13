using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Qgp.Api.Infrastructure.Persistence;

namespace Qgp.Api.Tests;

/// <summary>
/// Integration test B1 — doc-types chiều sâu (ADM-F-02). Loại tài liệu là bảng doc_types
/// (nguồn sự thật) thay enum: list active cho mọi user, CRUD admin.config, validate lúc tạo doc,
/// không xoá loại đang dùng. Cần Postgres.
/// </summary>
public class DocTypesTests(QgpApiFactory factory) : IClassFixture<QgpApiFactory>
{
    private readonly QgpApiFactory _factory = factory;

    private async Task<HttpClient> LoginAsync(string sub, params string[] roles)
    {
        var client = _factory.CreateClient();
        var res = await client.PostAsJsonAsync("/auth/dev-login", new { sub, roles });
        res.EnsureSuccessStatusCode();
        var token = (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("access_token").GetString();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    [Fact]
    public async Task Active_doc_types_seeded_and_listed_for_any_user()
    {
        var reader = await LoginAsync("dt-reader@fpt", "READER");
        var page = await reader.GetFromJsonAsync<JsonElement>("/v1/doc-types");
        var codes = page.GetProperty("data").EnumerateArray().Select(t => t.GetProperty("code").GetString()).ToArray();
        Assert.Contains("Process", codes);
        Assert.Contains("Work Instruction", codes); // giữ giá trị wire (dấu cách)
        Assert.True(codes.Length >= 7);

        // Reader không được vào quản trị doc-types.
        Assert.Equal(HttpStatusCode.Forbidden, (await reader.GetAsync("/v1/admin/doc-types")).StatusCode);
    }

    [Fact]
    public async Task Crud_new_type_used_by_doc_then_in_use_guard()
    {
        var qa = await LoginAsync("dt-qa@fpt", "QA_LEAD"); // QA_LEAD: vừa admin.config vừa doc.author
        var code = $"Guideline-{Guid.NewGuid():N}"[..18];
        var docId = $"IT-DT-{Guid.NewGuid():N}"[..18];
        string? typeId = null;
        try
        {
            // Tạo loại mới.
            var created = await (await qa.PostAsJsonAsync("/v1/admin/doc-types", new { code, label = "Hướng dẫn" })).Content.ReadFromJsonAsync<JsonElement>();
            typeId = created.GetProperty("id").GetString();
            Assert.Equal(code, created.GetProperty("code").GetString());

            // Tạo tài liệu dùng loại mới → hợp lệ (validate theo doc_types active).
            (await qa.PostAsJsonAsync("/v1/documents", new { doc_id = docId, title = "DT doc", type = code, content_markdown = "# x" })).EnsureSuccessStatusCode();

            // Doc detail phản ánh type = code.
            var doc = await qa.GetFromJsonAsync<JsonElement>($"/v1/documents/{docId}");
            Assert.Equal(code, doc.GetProperty("type").GetString());

            // Không xoá được loại đang dùng.
            Assert.Equal(HttpStatusCode.Conflict, (await qa.DeleteAsync($"/v1/admin/doc-types/{typeId}")).StatusCode);

            // Gỡ doc rồi xoá loại → 204.
            await CleanupDocAsync(docId);
            Assert.Equal(HttpStatusCode.NoContent, (await qa.DeleteAsync($"/v1/admin/doc-types/{typeId}")).StatusCode);
            typeId = null;
        }
        finally
        {
            await CleanupDocAsync(docId);
            if (typeId is not null) await qa.DeleteAsync($"/v1/admin/doc-types/{typeId}");
        }
    }

    [Fact]
    public async Task Create_doc_with_unknown_type_is_422()
    {
        var qa = await LoginAsync("dt-qa2@fpt", "QA_LEAD");
        var res = await qa.PostAsJsonAsync("/v1/documents", new { doc_id = $"IT-DTX-{Guid.NewGuid():N}"[..18], title = "x", type = "Nope-xyz", content_markdown = "# x" });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, res.StatusCode);
    }

    private async Task CleanupDocAsync(string docId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<QgpDbContext>();
        var doc = await db.Documents.FirstOrDefaultAsync(d => d.DocId == docId);
        if (doc is null) return;
        doc.CurrentEffectiveVersionId = null;
        await db.SaveChangesAsync();
        db.Documents.Remove(doc);
        await db.SaveChangesAsync();
    }
}
