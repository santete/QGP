namespace Qgp.Api.Domain.Entities;

/// <summary>
/// Loại tài liệu (ADM-F-02, doc-types) — NGUỒN SỰ THẬT thay cho enum cứng (B1 chiều sâu).
/// <see cref="Code"/> là giá trị dùng ở API/DB (documents.type)/Meilisearch filter (ổn định, không sửa);
/// <see cref="Label"/> để hiển thị. QA Lead/Admin thêm loại mới runtime.
/// </summary>
public class DocType : AuditableEntity
{
    public Guid Id { get; set; }
    public string Code { get; set; } = null!;   // ổn định — dùng ở documents.type + search filter
    public string Label { get; set; } = null!;
    public int Seq { get; set; }
    public bool Active { get; set; } = true;
}
