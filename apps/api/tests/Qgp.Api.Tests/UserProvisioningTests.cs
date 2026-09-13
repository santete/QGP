using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Qgp.Api.Infrastructure.Persistence;

namespace Qgp.Api.Tests;

/// <summary>
/// Integration test B0 — lớp CHIẾU user/role (UserProvisioningService). GET /me phải upsert user
/// + đồng bộ user_roles theo claim 'role' của token (mirror: thêm role mới, gỡ role cũ).
/// Nguồn authz vẫn là token; DB chỉ chiếu để B1 (xem user/role) + B2 (gửi thông báo) có dữ liệu.
/// Cần Postgres (docker compose up).
/// </summary>
public class UserProvisioningTests(QgpApiFactory factory) : IClassFixture<QgpApiFactory>
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

    private async Task<string[]> RolesInDbAsync(string sub)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<QgpDbContext>();
        return await db.UserRoles.AsNoTracking()
            .Where(ur => ur.User.SsoSubject == sub)
            .Select(ur => ur.Role.Code)
            .OrderBy(c => c)
            .ToArrayAsync();
    }

    [Fact]
    public async Task Me_projects_user_and_roles_into_db()
    {
        var sub = $"prov-{Guid.NewGuid():N}"[..18] + "@fpt";
        try
        {
            var client = await LoginAsync(sub, "AUTHOR", "READER");
            (await client.GetAsync("/me")).EnsureSuccessStatusCode();

            // User được upsert + user_roles = {AUTHOR, READER}.
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<QgpDbContext>();
                var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.SsoSubject == sub);
                Assert.NotNull(user);
                Assert.Equal(sub, user!.DisplayName); // dev token: name == sub
            }
            Assert.Equal(new[] { "AUTHOR", "READER" }, await RolesInDbAsync(sub));
        }
        finally { await CleanupAsync(sub); }
    }

    [Fact]
    public async Task Me_reconciles_stale_roles_on_relogin()
    {
        var sub = $"prov-{Guid.NewGuid():N}"[..18] + "@fpt";
        try
        {
            var c1 = await LoginAsync(sub, "AUTHOR", "APPROVER");
            (await c1.GetAsync("/me")).EnsureSuccessStatusCode();
            Assert.Equal(new[] { "APPROVER", "AUTHOR" }, await RolesInDbAsync(sub));

            // Đăng nhập lại với BỘ role khác (token mới) → mirror: gỡ AUTHOR/APPROVER, chỉ còn READER.
            var c2 = await LoginAsync(sub, "READER");
            (await c2.GetAsync("/me")).EnsureSuccessStatusCode();
            Assert.Equal(new[] { "READER" }, await RolesInDbAsync(sub));
        }
        finally { await CleanupAsync(sub); }
    }

    private async Task CleanupAsync(string sub)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<QgpDbContext>();
        var user = await db.Users.FirstOrDefaultAsync(u => u.SsoSubject == sub);
        if (user is not null)
        {
            var roles = db.UserRoles.Where(ur => ur.UserId == user.Id);
            db.UserRoles.RemoveRange(roles);
            await db.SaveChangesAsync();
            db.Users.Remove(user);
            await db.SaveChangesAsync();
        }
    }
}
