namespace Qgp.Api.Domain.Entities;

/// <summary>Lộ trình học theo role (ONB-F-01, SDD §2.3). Chuỗi tài liệu xếp thứ tự cho người mới.</summary>
public class LearningPath : AuditableEntity
{
    public Guid Id { get; set; }
    public Guid RoleId { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }

    public Role Role { get; set; } = null!;
    public ICollection<PathItem> Items { get; set; } = new List<PathItem>();
}

/// <summary>Một mục trong lộ trình — trỏ tới tài liệu, có thứ tự (seq) + cờ bắt buộc.</summary>
public class PathItem
{
    public Guid Id { get; set; }
    public Guid LearningPathId { get; set; }
    public Guid DocumentId { get; set; }
    public int Seq { get; set; }
    public bool Mandatory { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public LearningPath LearningPath { get; set; } = null!;
    public Document Document { get; set; } = null!;
}
