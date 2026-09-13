using Qgp.Api.Domain.Enums;

namespace Qgp.Api.Domain.Entities;

/// <summary>
/// Danh tính tài liệu (doc_id ổn định, BR-01 unique+immutable). Trỏ bản Effective
/// hiện hành qua <see cref="CurrentEffectiveVersionId"/> (BR-02).
/// </summary>
public class Document : AuditableEntity
{
    public Guid Id { get; set; }
    public string DocId { get; set; } = null!;          // BR-01 immutable, unique
    public string Title { get; set; } = null!;
    public string Type { get; set; } = null!;          // = doc_types.code (B1 chiều sâu, thay enum DocumentType)
    public string? Classification { get; set; }
    public Guid? CurrentEffectiveVersionId { get; set; } // BR-02 (nullable: chưa có Effective)
    public bool MandatoryAck { get; set; }
    public DateOnly? NextReviewDate { get; set; }

    public ICollection<DocumentVersion> Versions { get; set; } = new List<DocumentVersion>();
    public DocumentVersion? CurrentEffectiveVersion { get; set; }
    public ICollection<DocTag> DocTags { get; set; } = new List<DocTag>();
    public ICollection<DocAudienceRole> AudienceRoles { get; set; } = new List<DocAudienceRole>();
}

/// <summary>
/// Phiên bản + trạng thái vòng đời. Nội dung lưu ở Git (<see cref="ContentGitRef"/>, BR-03 immutable).
/// </summary>
public class DocumentVersion : AuditableEntity
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public string Version { get; set; } = null!;        // vd "2.1"
    public VersionStatus Status { get; set; }
    public DateOnly? IssueDate { get; set; }
    public DateOnly? EffectiveDate { get; set; }         // BR-07: >= issue_date
    public string? ChangeSummary { get; set; }           // BR-04
    public string? ContentGitRef { get; set; }           // BR-03 (khi tích hợp Git store)
    public string? ContentMarkdown { get; set; }         // Nội dung tạm ở DB (trước khi có Git store, E1.6)

    public Document Document { get; set; } = null!;
    public ICollection<Acknowledgement> Acknowledgements { get; set; } = new List<Acknowledgement>();
    public ICollection<Feedback> Feedback { get; set; } = new List<Feedback>();
}

/// <summary>Bảng nối document ↔ tag (composite PK).</summary>
public class DocTag
{
    public Guid DocumentId { get; set; }
    public Guid TagId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Document Document { get; set; } = null!;
    public Tag Tag { get; set; } = null!;
}

/// <summary>Role áp dụng của tài liệu — nguồn cho REC + reason explainable (BR-11).</summary>
public class DocAudienceRole
{
    public Guid DocumentId { get; set; }
    public Guid RoleId { get; set; }
    public string? Reason { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Document Document { get; set; } = null!;
    public Role Role { get; set; } = null!;
}

/// <summary>Quan hệ tài liệu (supersedes/depends_on/impacts) — cảnh báo tác động (DOC-F-07).</summary>
public class DocRelation : AuditableEntity
{
    public Guid Id { get; set; }
    public Guid SourceDocumentId { get; set; }
    public Guid TargetDocumentId { get; set; }
    public DocRelationType RelationType { get; set; }

    public Document SourceDocument { get; set; } = null!;
    public Document TargetDocument { get; set; } = null!;
}
