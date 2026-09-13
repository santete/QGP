using Microsoft.EntityFrameworkCore;
using Qgp.Api.Domain.Entities;
using Qgp.Api.Domain.Enums;

namespace Qgp.Api.Infrastructure.Persistence;

/// <summary>
/// DbContext của qgp-api. Schema <c>qgp</c> (R-SQL-DB-002: không dùng public),
/// snake_case (R-SQL-OBJ-001/002), id UUID gen_random_uuid (R-SQL-OBJ-003),
/// created_at/updated_at mọi bảng (R-SQL-OBJ-004). Ràng buộc governance: BR-01/02/07.
/// </summary>
public class QgpDbContext(DbContextOptions<QgpDbContext> options) : DbContext(options)
{
    public const string Schema = "qgp";

    // Thời điểm seed cố định (HasData yêu cầu giá trị hằng — không dùng DateTimeOffset.UtcNow).
    private static readonly DateTimeOffset SeedTime = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<DocType> DocTypes => Set<DocType>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentVersion> DocumentVersions => Set<DocumentVersion>();
    public DbSet<DocTag> DocTags => Set<DocTag>();
    public DbSet<DocAudienceRole> DocAudienceRoles => Set<DocAudienceRole>();
    public DbSet<DocRelation> DocRelations => Set<DocRelation>();
    public DbSet<Acknowledgement> Acknowledgements => Set<Acknowledgement>();
    public DbSet<Feedback> Feedback => Set<Feedback>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<LearningPath> LearningPaths => Set<LearningPath>();
    public DbSet<PathItem> PathItems => Set<PathItem>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.HasDefaultSchema(Schema);

        ConfigureIdentity(b);
        ConfigureDocuments(b);
        ConfigureEngagement(b);
        ConfigureOnboarding(b);
        ApplyColumnDefaults(b);
        SeedRoles(b);
        SeedDocTypes(b);
    }

    private static void ConfigureIdentity(ModelBuilder b)
    {
        b.Entity<User>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.SsoSubject).IsUnique().HasDatabaseName("uq_users_sso_subject");
            e.Property(x => x.SsoSubject).HasMaxLength(200).IsRequired();
            e.Property(x => x.DisplayName).HasMaxLength(200);
            e.Property(x => x.Email).HasMaxLength(320);
        });

        b.Entity<Role>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Code).IsUnique().HasDatabaseName("uq_roles_code");
            e.Property(x => x.Code).HasMaxLength(50).IsRequired();
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
        });

        b.Entity<UserRole>(e =>
        {
            e.HasKey(x => new { x.UserId, x.RoleId });
            e.HasOne(x => x.User).WithMany(u => u.UserRoles).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Role).WithMany(r => r.UserRoles).HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Tag>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Slug).IsUnique().HasDatabaseName("uq_tags_slug");
            e.Property(x => x.Slug).HasMaxLength(80).IsRequired();
            e.Property(x => x.Name).HasMaxLength(120).IsRequired();
        });

        b.Entity<DocType>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Code).IsUnique().HasDatabaseName("uq_doc_types_code");
            e.Property(x => x.Code).HasMaxLength(50).IsRequired();
            e.Property(x => x.Label).HasMaxLength(100).IsRequired();
        });
    }

    private static void ConfigureDocuments(ModelBuilder b)
    {
        b.Entity<Document>(e =>
        {
            e.HasKey(x => x.Id);
            // BR-01: doc_id duy nhất (immutable enforce ở tầng app).
            e.HasIndex(x => x.DocId).IsUnique().HasDatabaseName("uq_doc_id");
            e.Property(x => x.DocId).HasMaxLength(64).IsRequired();
            e.Property(x => x.Title).HasMaxLength(200).IsRequired();
            // Type = string tự do = doc_types.code (B1 chiều sâu, thay enum). 50 để chứa loại tuỳ biến.
            e.Property(x => x.Type).HasMaxLength(50).IsRequired();
            e.Property(x => x.Classification).HasMaxLength(50);

            // BR-02: trỏ bản Effective hiện hành (nullable, không cascade).
            e.HasOne(x => x.CurrentEffectiveVersion)
                .WithMany()
                .HasForeignKey(x => x.CurrentEffectiveVersionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<DocumentVersion>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Version).HasMaxLength(20).IsRequired();
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            e.Property(x => x.ChangeSummary);
            e.Property(x => x.ContentGitRef).HasMaxLength(200);

            e.HasOne(x => x.Document)
                .WithMany(d => d.Versions)
                .HasForeignKey(x => x.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            // BR-02: đúng 1 bản Effective mỗi doc (partial unique index).
            e.HasIndex(x => x.DocumentId)
                .IsUnique()
                .HasDatabaseName("uq_one_effective_per_doc")
                .HasFilter("status = 'Effective'");

            // Scheduler quét Published tới hạn (SDD §2.4).
            e.HasIndex(x => new { x.Status, x.EffectiveDate })
                .HasDatabaseName("document_versions_status_effective_date_idx");

            // BR-07: effective_date >= issue_date.
            e.ToTable(t => t.HasCheckConstraint(
                "ck_effective_ge_issue",
                "effective_date IS NULL OR issue_date IS NULL OR effective_date >= issue_date"));
        });

        b.Entity<DocTag>(e =>
        {
            e.HasKey(x => new { x.DocumentId, x.TagId });
            e.HasOne(x => x.Document).WithMany(d => d.DocTags).HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Tag).WithMany(t => t.DocTags).HasForeignKey(x => x.TagId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<DocAudienceRole>(e =>
        {
            e.HasKey(x => new { x.DocumentId, x.RoleId });
            e.Property(x => x.Reason).HasMaxLength(200);
            e.HasOne(x => x.Document).WithMany(d => d.AudienceRoles).HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Role).WithMany().HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Restrict);
            // REC theo role (SDD §2.4).
            e.HasIndex(x => x.RoleId).HasDatabaseName("doc_audience_roles_role_id_idx");
        });

        b.Entity<DocRelation>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.RelationType).HasConversion<string>().HasMaxLength(20).IsRequired();
            e.HasOne(x => x.SourceDocument).WithMany().HasForeignKey(x => x.SourceDocumentId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.TargetDocument).WithMany().HasForeignKey(x => x.TargetDocumentId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.SourceDocumentId, x.TargetDocumentId, x.RelationType })
                .IsUnique().HasDatabaseName("uq_doc_relation");
        });
    }

    private static void ConfigureEngagement(ModelBuilder b)
    {
        b.Entity<Acknowledgement>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Version).WithMany(v => v.Acknowledgements).HasForeignKey(x => x.VersionId).OnDelete(DeleteBehavior.Cascade);
            // 1 user ack 1 version tối đa 1 lần.
            e.HasIndex(x => new { x.UserId, x.VersionId }).IsUnique().HasDatabaseName("uq_ack_user_version");
        });

        b.Entity<Feedback>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Category).HasConversion<string>().HasMaxLength(30).IsRequired();
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            e.HasOne(x => x.Version).WithMany(v => v.Feedback).HasForeignKey(x => x.VersionId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            // Triage board + RPT-F-05 (SDD §2.4).
            e.HasIndex(x => new { x.Status, x.CreatedAt }).HasDatabaseName("feedback_status_created_at_idx");
        });

        b.Entity<AuditLog>(e =>
        {
            // Partition theo tháng RANGE(at) — PK phải chứa cột partition (id, at) (SDD §2.4).
            e.HasKey(x => new { x.Id, x.At });
            e.Property(x => x.Action).HasMaxLength(100).IsRequired();
            e.HasIndex(x => x.At).HasDatabaseName("audit_log_at_idx");
            e.HasIndex(x => x.DocumentId).HasDatabaseName("audit_log_document_id_idx");
        });

        b.Entity<Notification>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Type).HasMaxLength(40).IsRequired();
            e.Property(x => x.Title).HasMaxLength(300).IsRequired();
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            // FK tới tài liệu (nullable) — xoá tài liệu thì dọn luôn thông báo liên quan (tránh doc_id treo).
            e.HasOne<Document>().WithMany().HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
            // Bell: hộp thư của user, chưa đọc trước (ADM-F-04).
            e.HasIndex(x => new { x.UserId, x.ReadAt, x.CreatedAt }).HasDatabaseName("notifications_user_read_created_idx");
        });

        b.Entity<Subscription>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Document).WithMany().HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
            // 1 user theo dõi 1 tài liệu tối đa 1 lần.
            e.HasIndex(x => new { x.UserId, x.DocumentId }).IsUnique().HasDatabaseName("uq_subscription_user_doc");
        });
    }

    private static void ConfigureOnboarding(ModelBuilder b)
    {
        b.Entity<LearningPath>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Title).HasMaxLength(200).IsRequired();
            e.HasOne(x => x.Role).WithMany().HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Cascade);
            // ONB-F-01: 1 lộ trình / role (gộp item nếu cần nhiều).
            e.HasIndex(x => x.RoleId).IsUnique().HasDatabaseName("uq_learning_path_role");
        });

        b.Entity<PathItem>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.LearningPath).WithMany(p => p.Items).HasForeignKey(x => x.LearningPathId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Document).WithMany().HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
            // Không lặp tài liệu trong 1 lộ trình.
            e.HasIndex(x => new { x.LearningPathId, x.DocumentId }).IsUnique().HasDatabaseName("uq_path_item_doc");
            e.HasIndex(x => new { x.LearningPathId, x.Seq }).HasDatabaseName("path_items_path_seq_idx");
        });
    }

    /// <summary>R-SQL-OBJ-003 (id UUID gen_random_uuid) + R-SQL-OBJ-004 (timestamps default now()).</summary>
    private static void ApplyColumnDefaults(ModelBuilder b)
    {
        foreach (var entity in b.Model.GetEntityTypes())
        {
            if (entity.FindProperty("Id") is { ClrType: var t } idProp && t == typeof(Guid))
                idProp.SetDefaultValueSql("gen_random_uuid()");

            if (entity.FindProperty("CreatedAt") is { } created)
                created.SetDefaultValueSql("now()");

            if (entity.FindProperty("UpdatedAt") is { } updated)
                updated.SetDefaultValueSql("now()");
        }
    }

    /// <summary>Seed 6 role RBAC (SDD §6.2 / PRD §10.1). Guid cố định để migration idempotent.</summary>
    private void SeedRoles(ModelBuilder b)
    {
        (string id, string code, string name)[] roles =
        [
            ("11111111-1111-1111-1111-111111111101", "READER", "Reader"),
            ("11111111-1111-1111-1111-111111111102", "CONTRIBUTOR", "Contributor"),
            ("11111111-1111-1111-1111-111111111103", "AUTHOR", "Author"),
            ("11111111-1111-1111-1111-111111111104", "APPROVER", "Approver"),
            ("11111111-1111-1111-1111-111111111105", "QA_LEAD", "QA Lead"),
            ("11111111-1111-1111-1111-111111111106", "ADMIN", "Admin"),
        ];

        b.Entity<Role>().HasData(roles.Select(r => new Role
        {
            Id = Guid.Parse(r.id),
            Code = r.code,
            Name = r.name,
            CreatedAt = SeedTime,
            UpdatedAt = SeedTime,
        }));
    }

    /// <summary>
    /// Seed 7 loại tài liệu chuẩn (B1 chiều sâu — thay enum DocumentType cũ). Code = giá trị wire
    /// đang dùng (giữ nguyên "Work Instruction" có dấu cách để không phá dữ liệu/UI hiện có).
    /// QA Lead/Admin thêm loại mới runtime qua /v1/admin/doc-types.
    /// </summary>
    private void SeedDocTypes(ModelBuilder b)
    {
        (string id, string code)[] types =
        [
            ("22222222-2222-2222-2222-222222222201", "Policy"),
            ("22222222-2222-2222-2222-222222222202", "Process"),
            ("22222222-2222-2222-2222-222222222203", "Procedure"),
            ("22222222-2222-2222-2222-222222222204", "Work Instruction"),
            ("22222222-2222-2222-2222-222222222205", "Template"),
            ("22222222-2222-2222-2222-222222222206", "Checklist"),
            ("22222222-2222-2222-2222-222222222207", "Standard"),
        ];

        b.Entity<DocType>().HasData(types.Select((t, i) => new DocType
        {
            Id = Guid.Parse(t.id),
            Code = t.code,
            Label = t.code,
            Seq = i + 1,
            Active = true,
            CreatedAt = SeedTime,
            UpdatedAt = SeedTime,
        }));
    }
}
