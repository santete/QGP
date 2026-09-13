using Microsoft.EntityFrameworkCore;
using Qgp.Api.Domain.Entities;
using Qgp.Api.Domain.Enums;

namespace Qgp.Api.Infrastructure.Persistence;

/// <summary>
/// Seed dữ liệu mẫu cho dev/demo (QA-PROC-005 có bản Effective) — để slice S4 đọc dữ liệu thật.
/// Idempotent: bỏ qua nếu tài liệu đã tồn tại. CHỈ chạy ở môi trường Development.
/// </summary>
public static class DevDataSeeder
{
    private const string SampleMarkdown = """
# Quy trình Đảm bảo Chất lượng Phần mềm

Tài liệu mô tả quy trình QA 8 bước áp dụng cho mọi dự án phần mềm nội bộ,
kèm hai chốt kiểm soát chất lượng (Quality Gate).

## Phạm vi
Áp dụng cho tất cả nhóm phát triển thuộc khối Công nghệ.

## Quy trình 8 bước
1. Tiếp nhận & phân tích yêu cầu
2. Lập kế hoạch kiểm thử
3. Thiết kế test case
4. Chuẩn bị môi trường & dữ liệu
5. Thực thi kiểm thử
6. Ghi nhận & theo dõi lỗi
7. Kiểm thử hồi quy
8. Báo cáo & nghiệm thu

## Quality Gate
> **QG1** — Hoàn tất trước khi vào phát triển: tiêu chí chấp nhận rõ ràng, test plan được duyệt.
>
> **QG2** — Hoàn tất trước khi phát hành: 0 lỗi nghiêm trọng, độ phủ kiểm thử ≥ 80%.
""";

    public static async Task SeedAsync(QgpDbContext db, CancellationToken ct = default)
    {
        await SeedSampleDocAsync(db, ct);
        await SeedOnboardingPathAsync(db, ct); // độc lập — chạy cả khi doc mẫu đã tồn tại từ trước
    }

    private static async Task SeedSampleDocAsync(QgpDbContext db, CancellationToken ct)
    {
        const string docId = "QA-PROC-005";
        if (await db.Documents.AnyAsync(d => d.DocId == docId, ct)) return;

        var reader = await db.Roles.FirstOrDefaultAsync(r => r.Code == "READER", ct);

        var doc = new Document
        {
            DocId = docId,
            Title = "Quy trình Đảm bảo Chất lượng Phần mềm",
            Type = "Process",
            Classification = "Internal",
            MandatoryAck = true,
            NextReviewDate = new DateOnly(2027, 1, 15),
        };
        doc.DocTags.Add(new DocTag { Tag = new Tag { Slug = "qa", Name = "qa" } });
        doc.DocTags.Add(new DocTag { Tag = new Tag { Slug = "quality-gate", Name = "quality-gate" } });
        if (reader is not null)
            doc.AudienceRoles.Add(new DocAudienceRole { Role = reader, Reason = "Bắt buộc cho toàn bộ kỹ sư" });

        var superseded = new DocumentVersion
        {
            Version = "2.0",
            Status = VersionStatus.Superseded,
            IssueDate = new DateOnly(2025, 12, 1),
            EffectiveDate = new DateOnly(2025, 12, 15),
            ChangeSummary = "Ban hành quy trình 8 bước",
            ContentMarkdown = SampleMarkdown,
        };
        var effective = new DocumentVersion
        {
            Version = "2.1",
            Status = VersionStatus.Effective,
            IssueDate = new DateOnly(2026, 6, 1),
            EffectiveDate = new DateOnly(2026, 6, 15),
            ChangeSummary = "Cập nhật tiêu chí Quality Gate 1",
            ContentMarkdown = SampleMarkdown,
        };
        doc.Versions.Add(superseded);
        doc.Versions.Add(effective);

        db.Documents.Add(doc);
        await db.SaveChangesAsync(ct);

        // Trỏ bản Effective hiện hành (BR-02) sau khi có Id.
        doc.CurrentEffectiveVersionId = effective.Id;
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Lộ trình onboarding mẫu cho READER (ONB-F-01) — 1 item QA-PROC-005. Idempotent, độc lập với seed doc.</summary>
    private static async Task SeedOnboardingPathAsync(QgpDbContext db, CancellationToken ct)
    {
        var reader = await db.Roles.FirstOrDefaultAsync(r => r.Code == "READER", ct);
        if (reader is null) return;
        if (await db.LearningPaths.AnyAsync(p => p.RoleId == reader.Id, ct)) return;

        var doc = await db.Documents.FirstOrDefaultAsync(d => d.DocId == "QA-PROC-005", ct);
        if (doc is null) return;

        var path = new LearningPath
        {
            RoleId = reader.Id,
            Title = "Nhập môn Chất lượng cho kỹ sư mới",
            Description = "Tài liệu nền tảng cần đọc khi gia nhập.",
        };
        path.Items.Add(new PathItem { DocumentId = doc.Id, Seq = 1, Mandatory = true });
        db.LearningPaths.Add(path);
        await db.SaveChangesAsync(ct);
    }
}
