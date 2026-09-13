using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace Qgp.Api.Auth;

/// <summary>Endpoint auth: dev-login (mock OIDC), /me, và demo RBAC /admin/ping.</summary>
public static class AuthEndpoints
{
    public sealed record DevLoginRequest(string Sub, string[] Roles);

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        // POST /auth/dev-login — CHỈ Dev mode (DevTokenIssuer chỉ đăng ký khi Mode=Dev).
        // [FromServices] BẮT BUỘC: ép resolve từ DI → null khi Mode=Oidc (nếu không, minimal API
        // coi issuer là body param thứ 2 → "Failure to infer parameters" → app CRASH lúc start ở Oidc.
        app.MapPost("/auth/dev-login", (DevLoginRequest req, [FromServices] DevTokenIssuer? issuer) =>
        {
            if (issuer is null)
                return Results.NotFound(Err("AUTH_DEV_DISABLED", "dev-login chỉ bật ở môi trường Dev"));

            if (string.IsNullOrWhiteSpace(req.Sub))
                return Results.UnprocessableEntity(Err("VALIDATION_ERROR", "sub bắt buộc"));

            var roles = (req.Roles ?? []).Distinct().ToArray();
            var invalid = roles.Except(QgpRoles.All).ToArray();
            if (invalid.Length > 0)
                return Results.UnprocessableEntity(Err("VALIDATION_ERROR", $"role không hợp lệ: {string.Join(",", invalid)}"));

            var token = issuer.Issue(req.Sub, roles);
            return Results.Ok(new
            {
                access_token = token,
                token_type = "Bearer",
                expires_in = DevTokenIssuer.ExpiresSeconds,
                sub = req.Sub,
                roles,
            });
        });

        // GET /me — thông tin phiên hiện tại (yêu cầu đăng nhập). Đồng thời CHIẾU danh tính vào DB (B0):
        // upsert user + đồng bộ user_roles theo token → B1 xem user/role, B2 gửi thông báo cho QA_LEAD/ADMIN.
        // Authz vẫn đọc từ token (Keycloak = nguồn sự thật); đây chỉ là projection.
        app.MapGet("/me", async (ClaimsPrincipal user, Application.IUserProvisioningService provisioning, CancellationToken ct) =>
        {
            var sub = user.FindFirst("sub")?.Value;
            var roles = user.FindAll("role").Select(c => c.Value).Distinct().ToArray();
            // display_name: ưu tiên "name" (dev + Keycloak) rồi "preferred_username" (Keycloak).
            var displayName = user.FindFirst("name")?.Value ?? user.FindFirst("preferred_username")?.Value;
            var email = user.FindFirst("email")?.Value;
            if (!string.IsNullOrEmpty(sub))
                await provisioning.SyncAsync(sub, displayName, email, roles, ct);

            return Results.Ok(new { sub, roles });
        }).RequireAuthorization();

        // GET /admin/ping — demo RBAC: cần policy admin.config (QA_LEAD/ADMIN).
        app.MapGet("/admin/ping", () => Results.Ok(new { ok = true }))
            .RequireAuthorization(QgpPolicies.AdminConfig);

        // POST /admin/reindex — rebuild Meilisearch index từ DB (ops). Sửa drift index.
        app.MapPost("/admin/reindex", async (Application.ISearchService search, CancellationToken ct) =>
                Results.Ok(new { indexed = await search.ReindexAllAsync(ct) }))
            .RequireAuthorization(QgpPolicies.AdminConfig);

        return app;
    }

    // Error envelope theo openapi.yaml: { error: { code, message } }.
    private static object Err(string code, string message) => new { error = new { code, message } };
}
