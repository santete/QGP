/** Mã role RBAC — mirror BE (Qgp.Api/Auth/QgpRoles) + seed qgp.roles.code (§10.1). */
export const ROLES = {
  READER: 'READER',
  CONTRIBUTOR: 'CONTRIBUTOR',
  AUTHOR: 'AUTHOR',
  APPROVER: 'APPROVER',
  QA_LEAD: 'QA_LEAD',
  ADMIN: 'ADMIN',
} as const;

export type Role = (typeof ROLES)[keyof typeof ROLES];

export const ALL_ROLES: Role[] = [
  ROLES.READER,
  ROLES.CONTRIBUTOR,
  ROLES.AUTHOR,
  ROLES.APPROVER,
  ROLES.QA_LEAD,
  ROLES.ADMIN,
];

/** Nhãn tiếng Việt cho role (hiển thị ở UI). */
export const ROLE_LABEL: Record<Role, string> = {
  READER: 'Người đọc',
  CONTRIBUTOR: 'Cộng tác viên',
  AUTHOR: 'Tác giả',
  APPROVER: 'Người duyệt',
  QA_LEAD: 'QA Governance Lead',
  ADMIN: 'Quản trị',
};

/** Tập role được phép cho khu vực quản trị (mirror policy admin.config §10.1). */
export const ADMIN_ROLES: Role[] = [ROLES.QA_LEAD, ROLES.ADMIN];

/** Tập role được duyệt tài liệu (mirror policy doc.approve §10.1). */
export const APPROVE_ROLES: Role[] = [ROLES.APPROVER, ROLES.QA_LEAD];

/** Tập role được soạn/sửa tài liệu (mirror policy doc.author §10.1). */
export const AUTHOR_ROLES: Role[] = [ROLES.AUTHOR, ROLES.APPROVER, ROLES.QA_LEAD];
