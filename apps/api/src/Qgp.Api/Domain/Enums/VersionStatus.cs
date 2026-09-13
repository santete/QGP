namespace Qgp.Api.Domain.Enums;

/// <summary>
/// Trạng thái vòng đời version (state machine SDD §4.3). Lưu DB dạng text.
/// 7 giá trị public khớp openapi.yaml <c>VersionStatus</c>; thêm
/// <see cref="UnderRevision"/> là trạng thái nội bộ khi tạo revision (SDD §4.3).
/// </summary>
public enum VersionStatus
{
    Draft,
    InReview,
    Approved,
    Published,
    Effective,
    Superseded,
    Retired,
    UnderRevision,
}
