using Markdig;
using Qgp.Api.Contracts;
using Qgp.Api.Domain.Entities;
using Qgp.Api.Domain.Enums;

namespace Qgp.Api.Application;

/// <summary>Chuyển đổi enum ↔ wire value (khớp openapi), render markdown, map entity → DTO.</summary>
public static class Mapping
{
    private static readonly MarkdownPipeline MdPipeline =
        new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

    // Loại tài liệu (Type) giờ là string tự do = doc_types.code (B1 chiều sâu) → không map enum nữa.

    // UnderRevision là state nội bộ (SDD §4.3) — không có trong openapi → phơi ra như Draft.
    public static string StatusToWire(VersionStatus s) =>
        s == VersionStatus.UnderRevision ? "Draft" : s.ToString();

    // FeedbackCategory enum ↔ wire snake_case (content_error…). Status trùng tên enum (New/Triaged…).
    public static string FeedbackCategoryToWire(FeedbackCategory c) => c switch
    {
        FeedbackCategory.ContentError => "content_error",
        FeedbackCategory.Unclear => "unclear",
        FeedbackCategory.Improvement => "improvement",
        _ => "question",
    };

    public static FeedbackCategory FeedbackCategoryFromWire(string s) => s switch
    {
        "content_error" => FeedbackCategory.ContentError,
        "unclear" => FeedbackCategory.Unclear,
        "improvement" => FeedbackCategory.Improvement,
        "question" => FeedbackCategory.Question,
        _ => throw AppException.Validation($"category không hợp lệ: {s}"),
    };

    public static FeedbackStatus FeedbackStatusFromWire(string s) =>
        Enum.TryParse<FeedbackStatus>(s, out var st) && Enum.IsDefined(st)
            ? st
            : throw AppException.Validation($"status không hợp lệ: {s}");

    public static string RenderHtml(string? markdown) =>
        string.IsNullOrWhiteSpace(markdown) ? "" : Markdown.ToHtml(markdown, MdPipeline);

    public static DocumentVersionDto ToVersionDto(DocumentVersion v, string docId) => new(
        v.Id,
        docId,
        v.Version,
        StatusToWire(v.Status),
        v.IssueDate,
        v.EffectiveDate,
        v.ChangeSummary,
        v.CreatedAt);
}
