namespace Qgp.Api.Auth;

/// <summary>Mã role RBAC — khớp seed <c>qgp.roles.code</c> (SDD §6.2 / PRD §10.1).</summary>
public static class QgpRoles
{
    public const string Reader = "READER";
    public const string Contributor = "CONTRIBUTOR";
    public const string Author = "AUTHOR";
    public const string Approver = "APPROVER";
    public const string QaLead = "QA_LEAD";
    public const string Admin = "ADMIN";

    public static readonly string[] All =
        [Reader, Contributor, Author, Approver, QaLead, Admin];
}

/// <summary>
/// Policy phân quyền theo ma trận §10.1 (SDD §6.2). Mỗi policy = tập role được phép
/// (default deny; RequireRole thoả nếu user có BẤT KỲ role trong tập).
/// </summary>
public static class QgpPolicies
{
    public const string DocRead = "doc.read";           // Đọc Effective
    public const string KbContribute = "kb.contribute"; // Đóng góp KB
    public const string DocAuthor = "doc.author";       // Soạn/sửa DOC
    public const string DocApprove = "doc.approve";     // Duyệt DOC
    public const string AdminConfig = "admin.config";   // Cấu hình/audit

    /// <summary>Policy → role được phép (nguồn: bảng RBAC §10.1).</summary>
    public static readonly IReadOnlyDictionary<string, string[]> RolesFor = new Dictionary<string, string[]>
    {
        [DocRead] = QgpRoles.All,
        [KbContribute] = [QgpRoles.Contributor, QgpRoles.Author, QgpRoles.Approver, QgpRoles.QaLead, QgpRoles.Admin],
        [DocAuthor] = [QgpRoles.Author, QgpRoles.Approver, QgpRoles.QaLead],
        [DocApprove] = [QgpRoles.Approver, QgpRoles.QaLead],
        [AdminConfig] = [QgpRoles.QaLead, QgpRoles.Admin],
    };
}
