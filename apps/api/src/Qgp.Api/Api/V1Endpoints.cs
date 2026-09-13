using System.Security.Claims;
using Qgp.Api.Application;
using Qgp.Api.Auth;
using Qgp.Api.Contracts;

namespace Qgp.Api.Api;

/// <summary>Endpoint /v1 cho DOC lifecycle (WF-01) + đọc (slice S4). RBAC §10.1 gắn theo từng route.</summary>
public static class V1Endpoints
{
    public static IEndpointRouteBuilder MapV1(this IEndpointRouteBuilder app)
    {
        var v1 = app.MapGroup("/v1").RequireAuthorization();

        // ---- Documents ---------------------------------------------------
        v1.MapGet("/documents", async (string? q, string? type, string? tag, int? limit, IDocumentService svc, CancellationToken ct) =>
                Results.Ok(new Page<DocumentSummaryDto>(await svc.ListAsync(q, type, tag, limit ?? 50, ct), null)))
            .RequireAuthorization(QgpPolicies.DocRead);

        // Full-text (Meilisearch, chỉ Effective — BR-06/NFR-02).
        v1.MapGet("/search", async (string? q, string? type, string? tag, int? limit, ISearchService svc, CancellationToken ct) =>
                Results.Ok(new Page<SearchHitDto>(await svc.SearchAsync(q, type, tag, limit ?? 50, ct), null)))
            .RequireAuthorization(QgpPolicies.DocRead);

        v1.MapGet("/documents/{docId}", async (string docId, IDocumentService svc, CancellationToken ct) =>
                Results.Ok(await svc.GetAsync(docId, ct)))
            .RequireAuthorization(QgpPolicies.DocRead);

        v1.MapGet("/documents/{docId}/versions", async (string docId, IDocumentService svc, CancellationToken ct) =>
                Results.Ok(await svc.ListVersionsAsync(docId, ct)))
            .RequireAuthorization(QgpPolicies.DocRead);

        // Diff 2 version (S6, DOC-F-06) — unified diff nội dung markdown.
        v1.MapGet("/documents/{docId}/diff", async (string docId, string from, string to, IDocumentService svc, CancellationToken ct) =>
                Results.Ok(await svc.GetDiffAsync(docId, from, to, ct)))
            .RequireAuthorization(QgpPolicies.DocRead);

        v1.MapPost("/documents", async (CreateDocumentRequest req, ClaimsPrincipal u, IDocumentService svc, CancellationToken ct) =>
            {
                var dto = await svc.CreateAsync(req, Sub(u), ct);
                return Results.Created($"/v1/documents/{dto.DocId}", dto);
            })
            .RequireAuthorization(QgpPolicies.DocAuthor);

        // Hàng đợi duyệt/ban hành — InReview (S9) hoặc Approved (S8). RBAC doc.approve (§10.1).
        v1.MapGet("/review-queue", async (string? status, int? limit, IDocumentService svc, CancellationToken ct) =>
            {
                var st = string.Equals(status, "Approved", StringComparison.OrdinalIgnoreCase)
                    ? Domain.Enums.VersionStatus.Approved
                    : Domain.Enums.VersionStatus.InReview;
                return Results.Ok(new Page<ReviewQueueItemDto>(await svc.ListReviewQueueAsync(st, limit ?? 50, ct), null));
            })
            .RequireAuthorization(QgpPolicies.DocApprove);

        // Feedback in-context (S13 gửi / S14 triage). Create: mọi user đăng nhập (doc.read).
        v1.MapPost("/feedback", async (CreateFeedbackRequest req, ClaimsPrincipal u, IFeedbackService svc, CancellationToken ct) =>
                Results.Created("/v1/feedback", await svc.CreateAsync(req, Sub(u), ct)))
            .RequireAuthorization(QgpPolicies.DocRead);

        v1.MapGet("/feedback", async (
                string? status, string? category,
                [Microsoft.AspNetCore.Mvc.FromQuery(Name = "doc_id")] string? docId,
                int? limit, IFeedbackService svc, CancellationToken ct) =>
                Results.Ok(new Page<FeedbackDto>(await svc.ListAsync(status, category, docId, limit ?? 50, ct), null)))
            .RequireAuthorization(QgpPolicies.AdminConfig);

        v1.MapPatch("/feedback/{id:guid}", async (Guid id, TriageFeedbackRequest req, ClaimsPrincipal u, IFeedbackService svc, CancellationToken ct) =>
                Results.Ok(await svc.TriageAsync(id, req, Sub(u), ct)))
            .RequireAuthorization(QgpPolicies.AdminConfig);

        // Gợi ý REC theo role (S2/S11, REC-F-02/UC-23, BR-11) — explainable, chỉ Effective trong quyền đọc.
        v1.MapGet("/recommendations", async (ClaimsPrincipal u, IRecommendationService svc, CancellationToken ct) =>
                Results.Ok(await svc.GetForUserAsync(Sub(u), Roles(u), ct)))
            .RequireAuthorization(QgpPolicies.DocRead);

        // Onboarding S12 (ONB-F-01/02/04) — lộ trình học theo role + tiến độ đọc. RBAC doc.read.
        v1.MapGet("/onboarding", async (ClaimsPrincipal u, IOnboardingService svc, CancellationToken ct) =>
                Results.Ok(await svc.GetForUserAsync(Sub(u), Roles(u), ct)))
            .RequireAuthorization(QgpPolicies.DocRead);

        // Báo cáo quản trị S15 (RPT-F-01/02/04) — aggregate. RBAC admin.config (QA_LEAD/ADMIN).
        v1.MapGet("/reports/issuance", async (IReportService svc, CancellationToken ct) =>
                Results.Ok(await svc.GetIssuanceAsync(ct)))
            .RequireAuthorization(QgpPolicies.AdminConfig);

        v1.MapGet("/reports/compliance", async (IReportService svc, CancellationToken ct) =>
                Results.Ok(await svc.GetComplianceAsync(ct)))
            .RequireAuthorization(QgpPolicies.AdminConfig);

        v1.MapGet("/reports/feedback", async (IReportService svc, CancellationToken ct) =>
                Results.Ok(await svc.GetFeedbackAsync(ct)))
            .RequireAuthorization(QgpPolicies.AdminConfig);

        v1.MapGet("/reports/access", async (IReportService svc, CancellationToken ct) =>
                Results.Ok(await svc.GetAccessAsync(ct)))
            .RequireAuthorization(QgpPolicies.AdminConfig);

        // Notifications S19 (ADM-F-04) — hộp thư của user (bell). RBAC doc.read (ai cũng có thông báo của mình).
        v1.MapGet("/notifications", async (
                [Microsoft.AspNetCore.Mvc.FromQuery(Name = "unread_only")] bool? unreadOnly,
                int? limit, ClaimsPrincipal u, INotificationService svc, CancellationToken ct) =>
                Results.Ok(await svc.ListAsync(Sub(u), unreadOnly ?? false, limit ?? 30, ct)))
            .RequireAuthorization(QgpPolicies.DocRead);

        v1.MapPost("/notifications/{id:guid}/read", async (Guid id, ClaimsPrincipal u, INotificationService svc, CancellationToken ct) =>
                Results.Ok(await svc.MarkReadAsync(id, Sub(u), ct)))
            .RequireAuthorization(QgpPolicies.DocRead);

        v1.MapPost("/notifications/read-all", async (ClaimsPrincipal u, INotificationService svc, CancellationToken ct) =>
                Results.Ok(new { updated = await svc.MarkAllReadAsync(Sub(u), ct) }))
            .RequireAuthorization(QgpPolicies.DocRead);

        // Theo dõi tài liệu (S19, ADM-F-04) — nhận thông báo khi có bản Effective mới. RBAC doc.read.
        v1.MapGet("/subscriptions", async (ClaimsPrincipal u, ISubscriptionService svc, CancellationToken ct) =>
                Results.Ok(new Page<SubscriptionDto>(await svc.ListAsync(Sub(u), ct), null)))
            .RequireAuthorization(QgpPolicies.DocRead);

        v1.MapPost("/documents/{docId}/subscribe", async (string docId, ClaimsPrincipal u, ISubscriptionService svc, CancellationToken ct) =>
                Results.Ok(await svc.SubscribeAsync(docId, Sub(u), ct)))
            .RequireAuthorization(QgpPolicies.DocRead);

        v1.MapDelete("/documents/{docId}/subscribe", async (string docId, ClaimsPrincipal u, ISubscriptionService svc, CancellationToken ct) =>
                Results.Ok(await svc.UnsubscribeAsync(docId, Sub(u), ct)))
            .RequireAuthorization(QgpPolicies.DocRead);

        // Audit trail (S16, ADM-F-03/NFR-03) — RBAC admin.config. actor resolve từ actor_id.
        v1.MapGet("/audit", async (
                [Microsoft.AspNetCore.Mvc.FromQuery(Name = "document_id")] Guid? documentId,
                DateTimeOffset? from, DateTimeOffset? to, int? limit,
                IAuditService svc, CancellationToken ct) =>
                Results.Ok(new Page<AuditEntryDto>(await svc.ListAsync(documentId, from, to, limit ?? 50, ct), null)))
            .RequireAuthorization(QgpPolicies.AdminConfig);

        // ---- Admin / S17 (ADM-F-01 RBAC read · ADM-F-02 taxonomy) — RBAC admin.config ----
        v1.MapGet("/admin/rbac", async (IAdminService svc, CancellationToken ct) =>
                Results.Ok(await svc.GetRbacAsync(ct)))
            .RequireAuthorization(QgpPolicies.AdminConfig);

        v1.MapGet("/admin/users", async (int? limit, IAdminService svc, CancellationToken ct) =>
                Results.Ok(new Page<AdminUserDto>(await svc.ListUsersAsync(limit ?? 100, ct), null)))
            .RequireAuthorization(QgpPolicies.AdminConfig);

        v1.MapGet("/admin/tags", async (IAdminService svc, CancellationToken ct) =>
                Results.Ok(new Page<TagDto>(await svc.ListTagsAsync(ct), null)))
            .RequireAuthorization(QgpPolicies.AdminConfig);

        v1.MapPost("/admin/tags", async (CreateTagRequest req, IAdminService svc, CancellationToken ct) =>
                Results.Created("/v1/admin/tags", await svc.CreateTagAsync(req, ct)))
            .RequireAuthorization(QgpPolicies.AdminConfig);

        v1.MapPatch("/admin/tags/{id:guid}", async (Guid id, UpdateTagRequest req, IAdminService svc, CancellationToken ct) =>
                Results.Ok(await svc.UpdateTagAsync(id, req, ct)))
            .RequireAuthorization(QgpPolicies.AdminConfig);

        v1.MapDelete("/admin/tags/{id:guid}", async (Guid id, IAdminService svc, CancellationToken ct) =>
            {
                await svc.DeleteTagAsync(id, ct);
                return Results.NoContent();
            })
            .RequireAuthorization(QgpPolicies.AdminConfig);

        // Loại tài liệu (ADM-F-02, doc-types). Danh sách ACTIVE cho form tạo/lọc — mọi user đăng nhập.
        v1.MapGet("/doc-types", async (IAdminService svc, CancellationToken ct) =>
                Results.Ok(new Page<DocTypeDto>(await svc.ListDocTypesAsync(activeOnly: true, ct), null)))
            .RequireAuthorization(QgpPolicies.DocRead);

        // Quản trị loại tài liệu (đầy đủ, kể cả inactive) — admin.config.
        v1.MapGet("/admin/doc-types", async (IAdminService svc, CancellationToken ct) =>
                Results.Ok(new Page<DocTypeDto>(await svc.ListDocTypesAsync(activeOnly: false, ct), null)))
            .RequireAuthorization(QgpPolicies.AdminConfig);

        v1.MapPost("/admin/doc-types", async (CreateDocTypeRequest req, IAdminService svc, CancellationToken ct) =>
                Results.Created("/v1/admin/doc-types", await svc.CreateDocTypeAsync(req, ct)))
            .RequireAuthorization(QgpPolicies.AdminConfig);

        v1.MapPatch("/admin/doc-types/{id:guid}", async (Guid id, UpdateDocTypeRequest req, IAdminService svc, CancellationToken ct) =>
                Results.Ok(await svc.UpdateDocTypeAsync(id, req, ct)))
            .RequireAuthorization(QgpPolicies.AdminConfig);

        v1.MapDelete("/admin/doc-types/{id:guid}", async (Guid id, IAdminService svc, CancellationToken ct) =>
            {
                await svc.DeleteDocTypeAsync(id, ct);
                return Results.NoContent();
            })
            .RequireAuthorization(QgpPolicies.AdminConfig);

        // Audience roles của 1 tài liệu (ADM-F-02) — xem/đặt lại tập role. RBAC admin.config.
        v1.MapGet("/documents/{docId}/audience", async (string docId, IDocumentService svc, CancellationToken ct) =>
                Results.Ok(new Page<DocAudienceItemDto>(await svc.GetAudienceAsync(docId, ct), null)))
            .RequireAuthorization(QgpPolicies.AdminConfig);

        v1.MapPut("/documents/{docId}/audience", async (string docId, SetDocAudienceRequest req, ClaimsPrincipal u, IDocumentService svc, CancellationToken ct) =>
                Results.Ok(new Page<DocAudienceItemDto>(await svc.SetAudienceAsync(docId, req, Sub(u), ct), null)))
            .RequireAuthorization(QgpPolicies.AdminConfig);

        // ---- Versions (lifecycle WF-01) ---------------------------------
        v1.MapPost("/documents/{docId}/versions", async (string docId, CreateVersionRequest req, ClaimsPrincipal u, IVersionService svc, CancellationToken ct) =>
            {
                var dto = await svc.CreateVersionAsync(docId, req, Sub(u), ct);
                return Results.Created($"/v1/documents/{docId}/versions", dto);
            })
            .RequireAuthorization(QgpPolicies.DocAuthor);

        // Editor S7 — đọc/sửa nội dung markdown của bản draft. RBAC doc.author.
        v1.MapGet("/versions/{id:guid}/content", async (Guid id, IVersionService svc, CancellationToken ct) =>
                Results.Ok(await svc.GetContentAsync(id, ct)))
            .RequireAuthorization(QgpPolicies.DocAuthor);

        v1.MapPatch("/versions/{id:guid}/content", async (Guid id, UpdateContentRequest req, ClaimsPrincipal u, IVersionService svc, CancellationToken ct) =>
                Results.Ok(await svc.UpdateContentAsync(id, req, Sub(u), ct)))
            .RequireAuthorization(QgpPolicies.DocAuthor);

        v1.MapPost("/versions/{id:guid}/submit", async (Guid id, ClaimsPrincipal u, IVersionService svc, CancellationToken ct) =>
                Results.Ok(await svc.SubmitAsync(id, Sub(u), ct)))
            .RequireAuthorization(QgpPolicies.DocAuthor);

        v1.MapPost("/versions/{id:guid}/approve", async (Guid id, ApproveRequest req, ClaimsPrincipal u, IVersionService svc, CancellationToken ct) =>
                Results.Ok(await svc.ApproveAsync(id, req, Sub(u), ct)))
            .RequireAuthorization(QgpPolicies.DocApprove);

        v1.MapPost("/versions/{id:guid}/publish", async (Guid id, PublishRequest req, ClaimsPrincipal u, IVersionService svc, CancellationToken ct) =>
                Results.Ok(await svc.PublishAsync(id, req, Sub(u), ct)))
            .RequireAuthorization(QgpPolicies.DocApprove);

        v1.MapPost("/versions/{id:guid}/acknowledge", async (Guid id, ClaimsPrincipal u, IVersionService svc, CancellationToken ct) =>
                Results.Ok(await svc.AcknowledgeAsync(id, Sub(u) ?? "unknown", ct)))
            .RequireAuthorization(QgpPolicies.DocRead);

        return app;
    }

    private static string? Sub(ClaimsPrincipal u) => u.FindFirst("sub")?.Value;

    // Role từ claim "role" (RoleClaimType cấu hình ở AuthSetup) — REC lọc theo audience_roles.
    private static IReadOnlyCollection<string> Roles(ClaimsPrincipal u) =>
        u.FindAll("role").Select(c => c.Value).Distinct().ToArray();
}
