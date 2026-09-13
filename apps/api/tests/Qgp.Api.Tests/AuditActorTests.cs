using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Qgp.Api.Infrastructure.Persistence;

namespace Qgp.Api.Tests;

/// <summary>
/// Audit actor mapping (Task 5a): audit_logs.actor_id = user_id theo sso_subject,
/// KHÔNG nhét actor vào cột action. Cần Postgres (docker compose up).
/// </summary>
public class AuditActorTests(QgpApiFactory factory) : IClassFixture<QgpApiFactory>
{
    private readonly QgpApiFactory _factory = factory;

    [Fact]
    public async Task Document_created_audit_has_actor_id_and_clean_action()
    {
        var sub = $"audit-actor-{Guid.NewGuid():N}@fpt";
        var client = _factory.CreateClient();
        var login = await client.PostAsJsonAsync("/auth/dev-login", new { sub, roles = new[] { "AUTHOR" } });
        var token = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("access_token").GetString();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var docId = $"IT-AUD-{Guid.NewGuid():N}"[..18];
        try
        {
            (await client.PostAsJsonAsync("/v1/documents", new
            {
                doc_id = docId,
                title = "Audit actor test",
                type = "Process",
                content_markdown = "# x",
            })).EnsureSuccessStatusCode();

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<QgpDbContext>();

            var doc = await db.Documents.FirstAsync(d => d.DocId == docId);
            var user = await db.Users.FirstAsync(u => u.SsoSubject == sub);
            var audit = await db.AuditLogs
                .Where(a => a.DocumentId == doc.Id && a.Action == "document.created")
                .FirstAsync();

            // actor_id map đúng user.
            Assert.Equal(user.Id, audit.ActorId);
            // action sạch — KHÔNG chứa "by <sub>".
            Assert.DoesNotContain(" by ", audit.Action);
        }
        finally
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<QgpDbContext>();
            var doc = db.Documents.FirstOrDefault(d => d.DocId == docId);
            if (doc is not null)
            {
                var logs = db.AuditLogs.Where(a => a.DocumentId == doc.Id);
                db.AuditLogs.RemoveRange(logs);
                doc.CurrentEffectiveVersionId = null;
                await db.SaveChangesAsync();
                db.Documents.Remove(doc);
                await db.SaveChangesAsync();
            }
        }
    }
}
