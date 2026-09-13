namespace Qgp.Api.Domain.Enums;

/// <summary>Phân loại feedback — khớp openapi.yaml <c>FeedbackCategory</c>.</summary>
public enum FeedbackCategory
{
    ContentError,
    Unclear,
    Improvement,
    Question,
}

/// <summary>Vòng đời feedback — khớp openapi.yaml <c>FeedbackStatus</c>.</summary>
public enum FeedbackStatus
{
    New,
    Triaged,
    InProgress,
    Resolved,
    Rejected,
}

/// <summary>Loại quan hệ tài liệu (doc_relations, DOC-F-07).</summary>
public enum DocRelationType
{
    Supersedes,
    DependsOn,
    Impacts,
}
