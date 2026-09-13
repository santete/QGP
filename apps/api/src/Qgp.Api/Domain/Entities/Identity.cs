namespace Qgp.Api.Domain.Entities;

/// <summary>Base cho mọi entity có audit timestamp (R-SQL-OBJ-004: created_at + updated_at).</summary>
public abstract class AuditableEntity
{
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>Người dùng (định danh qua SSO subject). PII tối thiểu (OD-3).</summary>
public class User : AuditableEntity
{
    public Guid Id { get; set; }
    public string SsoSubject { get; set; } = null!;
    public string? DisplayName { get; set; }
    public string? Email { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}

/// <summary>Vai trò RBAC (SDD §6.2 / PRD §10.1). code là mã ổn định.</summary>
public class Role : AuditableEntity
{
    public Guid Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}

/// <summary>Bảng nối user ↔ role (composite PK).</summary>
public class UserRole
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public User User { get; set; } = null!;
    public Role Role { get; set; } = null!;
}

/// <summary>Nhãn phân loại tài liệu.</summary>
public class Tag : AuditableEntity
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = null!;
    public string Name { get; set; } = null!;

    public ICollection<DocTag> DocTags { get; set; } = new List<DocTag>();
}
