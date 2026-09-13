using Qgp.Api.Domain.Enums;

namespace Qgp.Api.Domain.Entities;

/// <summary>Bằng chứng đã đọc tài liệu bắt buộc (DOC-F-09). Reset khi major (BR-05/08).</summary>
public class Acknowledgement : AuditableEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid VersionId { get; set; }
    public DateTimeOffset AckedAt { get; set; }

    public User User { get; set; } = null!;
    public DocumentVersion Version { get; set; } = null!;
}

/// <summary>Phản hồi in-context + vòng đời. Ngữ cảnh server-attached (FBK-F-04).</summary>
public class Feedback : AuditableEntity
{
    public Guid Id { get; set; }
    public Guid VersionId { get; set; }
    public Guid UserId { get; set; }
    public FeedbackCategory Category { get; set; }
    public FeedbackStatus Status { get; set; }
    public string? Body { get; set; }

    public DocumentVersion Version { get; set; } = null!;
    public User User { get; set; } = null!;
}

/// <summary>Theo dõi tài liệu (ADM-F-04, S19) — user nhận thông báo khi tài liệu có bản Effective mới.</summary>
public class Subscription
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid DocumentId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public User User { get; set; } = null!;
    public Document Document { get; set; } = null!;
}

/// <summary>Thông báo trong ứng dụng (ADM-F-04, S19) — sinh khi tài liệu trong audience/theo dõi của user trở thành Effective.</summary>
public class Notification : AuditableEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Type { get; set; } = null!;          // vd doc_effective (mở rộng: review_due…)
    public Guid? DocumentId { get; set; }
    public string Title { get; set; } = null!;
    public DateTimeOffset? ReadAt { get; set; }         // null = chưa đọc

    public User User { get; set; } = null!;
}

/// <summary>
/// Truy vết bất biến (ADM-F-03, NFR-03 ≥ 2 năm). Partition theo tháng (SDD §2.4) —
/// declarative partitioning bổ sung ở migration raw-SQL sau (xem gotcha T5).
/// </summary>
public class AuditLog
{
    public Guid Id { get; set; }
    public Guid? ActorId { get; set; }
    public string Action { get; set; } = null!;
    public Guid? DocumentId { get; set; }
    public DateTimeOffset At { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
