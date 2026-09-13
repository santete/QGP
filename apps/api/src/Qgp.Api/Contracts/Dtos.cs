namespace Qgp.Api.Contracts;

// DTO khớp product-spec/api/openapi.yaml (single source). Property PascalCase →
// snake_case qua JsonNamingPolicy.SnakeCaseLower (cấu hình ở Program). Enum để dạng
// string, giữ đúng wire value openapi (vd "Work Instruction", "content_error").

public sealed record ErrorBody(string Code, string Message, object? Details = null);
public sealed record ErrorResponse(ErrorBody Error);

/// <summary>List envelope: { data, next_cursor } (SDD §3.1).</summary>
public sealed record Page<T>(IReadOnlyList<T> Data, string? NextCursor);

public sealed record DocumentSummaryDto(
    string DocId,
    string Title,
    string Type,
    string? Classification,
    string? EffectiveVersion,
    string Status,
    IReadOnlyList<string> Tags);

public sealed record DocumentDetailDto(
    string DocId,
    string Title,
    string Type,
    string? Classification,
    string? EffectiveVersion,
    string Status,
    IReadOnlyList<string> Tags,
    bool MandatoryAck,
    DateOnly? NextReviewDate,
    IReadOnlyList<string> AudienceRoles,
    bool IsEffective,
    string? Badge,
    string? EffectiveLink,
    string ContentHtml);

public sealed record DocumentVersionDto(
    Guid Id,
    string DocId,
    string Version,
    string Status,
    DateOnly? IssueDate,
    DateOnly? EffectiveDate,
    string? ChangeSummary,
    DateTimeOffset CreatedAt);

public sealed record CreateDocumentRequest(
    string DocId,
    string Title,
    string Type,
    string? Classification,
    bool? MandatoryAck,
    DateOnly? NextReviewDate,
    IReadOnlyList<string>? AudienceRoles,
    IReadOnlyList<string>? Tags,
    string? ContentMarkdown);

public sealed record CreateVersionRequest(string ChangeType, string? ContentMarkdown);

public sealed record PublishRequest(DateOnly IssueDate, DateOnly EffectiveDate, string ChangeSummary);

public sealed record ApproveRequest(string Decision, string? Comment);

public sealed record AcknowledgementDto(Guid Id, Guid VersionId, DateTimeOffset AckedAt);

public sealed record DiffResultDto(
    string From,
    string To,
    string Diff,
    string? ChangeSummary);

public sealed record VersionContentDto(
    Guid VersionId,
    string DocId,
    string Version,
    string Status,
    string ContentMarkdown);

public sealed record UpdateContentRequest(string ContentMarkdown);

public sealed record FeedbackDto(
    Guid Id,
    Guid VersionId,
    string? DocId,
    string Version,
    string Category,
    string Status,
    string? Body,
    DateTimeOffset CreatedAt);

public sealed record CreateFeedbackRequest(string DocId, string Version, string? Url, string Category, string? Body);

public sealed record TriageFeedbackRequest(string Status, string? Note);

public sealed record AuditEntryDto(
    Guid Id,
    string? Actor,
    string Action,
    Guid? DocumentId,
    DateTimeOffset At);

public sealed record ReviewQueueItemDto(
    Guid VersionId,
    string DocId,
    string Title,
    string Version,
    DateTimeOffset SubmittedAt);

public sealed record SearchHitDto(
    string DocId,
    string Version,
    string Title,
    string Snippet,
    double Score);

public sealed record PublishResultDto(
    Guid VersionId,
    string DocId,
    string Version,
    string Status,
    DateOnly? EffectiveDate,
    string? Badge);

/// <summary>Một gợi ý REC (BR-11) — <c>Reason</c> giải thích được (bắt buộc / cần đọc lại / khớp vai trò / vừa cập nhật).</summary>
public sealed record RecommendationItemDto(
    string DocId,
    string Version,
    string Title,
    DateOnly? EffectiveDate,
    string Reason);

/// <summary>Kết quả REC theo role (SDD §5.2) — 2 nhóm: bắt buộc đọc và gợi ý.</summary>
public sealed record RecommendationsDto(
    IReadOnlyList<RecommendationItemDto> MustRead,
    IReadOnlyList<RecommendationItemDto> Suggested);

/// <summary>Một mục trong lộ trình onboarding (ONB-F-01) + trạng thái đã đọc (DOC-F-09).</summary>
public sealed record OnboardingItemDto(
    string DocId,
    string Title,
    int Seq,
    bool Mandatory,
    string? EffectiveVersion,
    bool Acked);

/// <summary>Tiến độ đọc lộ trình (ONB-F-02) — percent = read/total (0..100).</summary>
public sealed record OnboardingProgressDto(int Total, int Read, int Percent);

/// <summary>Lộ trình onboarding gộp theo role của user (ONB-F-01/02/04, S12).</summary>
public sealed record OnboardingDto(
    string? Title,
    IReadOnlyList<string> Roles,
    OnboardingProgressDto Progress,
    IReadOnlyList<OnboardingItemDto> Items);

/// <summary>Báo cáo ban hành + sức khoẻ tài liệu (RPT-F-01/02, S15). Aggregate — không lộ hành vi cá nhân.</summary>
public sealed record IssuanceReportDto(
    int TotalDocuments,
    int EffectiveCount,
    IReadOnlyDictionary<string, int> ByType,
    IReadOnlyDictionary<string, int> ByVersionStatus,
    int IssuedThisMonth,
    int OverdueReview,
    int DueSoonReview,
    int WithChangeSummaryPercent);

/// <summary>Compliance ack cho 1 tài liệu bắt buộc (RPT-F-04). not_read = sso_subject trong audience chưa ack.</summary>
public sealed record ComplianceItemDto(
    string DocId,
    string Title,
    string? EffectiveVersion,
    int AudienceCount,
    int AckedCount,
    int Percent,
    IReadOnlyList<string> NotRead);

/// <summary>Báo cáo compliance acknowledgement (RPT-F-04, S15) — chỉ tài liệu bắt buộc có bản Effective.</summary>
public sealed record ComplianceReportDto(
    int MandatoryDocs,
    int FullyCompliantDocs,
    IReadOnlyList<ComplianceItemDto> Items);

/// <summary>Thống kê feedback (RPT-F-05, S15) — theo trạng thái + thời gian xử lý TB.</summary>
public sealed record FeedbackReportDto(
    int Total,
    IReadOnlyDictionary<string, int> ByStatus,
    double? AvgResolutionHours,
    int ResolvedCount);

/// <summary>1 dòng access proxy (RPT-F-03) — tài liệu + số lượt xác nhận đọc.</summary>
public sealed record AccessItemDto(string DocId, string Title, int AckCount);

/// <summary>
/// Báo cáo truy cập (RPT-F-03, S15) — AGGREGATE (privacy §6). Hiện dùng proxy theo lượt ack
/// vì CHƯA có event lượt xem/tra cứu (doc_viewed/search_performed — P3). Note nêu rõ giới hạn.
/// </summary>
public sealed record AccessReportDto(
    int EffectiveWithZeroAck,
    IReadOnlyList<AccessItemDto> ZeroAckDocs,
    IReadOnlyList<AccessItemDto> TopEngaged,
    string Note);

/// <summary>Thông báo trong ứng dụng (ADM-F-04, S19).</summary>
public sealed record NotificationDto(
    Guid Id,
    string Type,
    string? DocId,
    string Title,
    bool Read,
    DateTimeOffset CreatedAt);

/// <summary>Trang thông báo + số chưa đọc (cho bell S19).</summary>
public sealed record NotificationListDto(
    IReadOnlyList<NotificationDto> Data,
    int UnreadCount);

/// <summary>Tài liệu đang theo dõi (ADM-F-04, S19).</summary>
public sealed record SubscriptionDto(
    string DocId,
    string Title,
    DateTimeOffset SubscribedAt);

/// <summary>Trạng thái theo dõi 1 tài liệu (toggle S19).</summary>
public sealed record SubscriptionStatusDto(string DocId, bool Subscribed);

// ── Admin / S17 (ADM-F-01 RBAC · ADM-F-02 taxonomy) ────────────────
/// <summary>Vai trò RBAC (§10.1) — code ổn định + tên hiển thị.</summary>
public sealed record RoleDto(string Code, string Name);

/// <summary>1 dòng ma trận RBAC: policy → các role được phép (§10.1).</summary>
public sealed record RbacPolicyDto(string Policy, IReadOnlyList<string> Roles);

/// <summary>Ma trận phân quyền §10.1 — CHỈ ĐỌC (nguồn authz thật là Keycloak).</summary>
public sealed record RbacMatrixDto(IReadOnlyList<RoleDto> Roles, IReadOnlyList<RbacPolicyDto> Policies);

/// <summary>User trong app (lớp chiếu B0) + role hiện có. Sửa role làm ở Keycloak, không ở đây.</summary>
public sealed record AdminUserDto(string Sub, string? DisplayName, string? Email, IReadOnlyList<string> Roles);

/// <summary>Tag/nhãn phân loại (ADM-F-02) — doc_count = số tài liệu đang gắn.</summary>
public sealed record TagDto(Guid Id, string Slug, string Name, int DocCount);
public sealed record CreateTagRequest(string Slug, string Name);
public sealed record UpdateTagRequest(string? Slug, string? Name);

/// <summary>Loại tài liệu (ADM-F-02, doc-types) — code là giá trị dùng ở API/DB/search; label để hiển thị.</summary>
public sealed record DocTypeDto(Guid Id, string Code, string Label, int Seq, bool Active, int DocCount);
public sealed record CreateDocTypeRequest(string Code, string? Label, int? Seq);
public sealed record UpdateDocTypeRequest(string? Label, int? Seq, bool? Active);

/// <summary>Audience roles của 1 tài liệu (ADM-F-02) — nguồn cho REC/notification (BR-11).</summary>
public sealed record DocAudienceItemDto(string Role, string? Reason);
public sealed record SetDocAudienceRequest(IReadOnlyList<DocAudienceItemDto> Audience);
