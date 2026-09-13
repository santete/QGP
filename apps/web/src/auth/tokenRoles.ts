import { ROLES, type Role } from './roles';

/**
 * Đọc claim từ access token Keycloak (A1b) — CHỈ để hiển thị UI + guard client-side (defense-in-depth).
 * KHÔNG verify chữ ký ở FE: server (A1a) mới là nơi xác thực token thật cho mọi request.
 */

/** Keycloak realm role (lowercase) → app Role. Mirror BE Auth:RoleMap (appsettings.json). */
const KEYCLOAK_ROLE_MAP: Record<string, Role> = {
  reader: ROLES.READER,
  contributor: ROLES.CONTRIBUTOR,
  author: ROLES.AUTHOR,
  approver: ROLES.APPROVER,
  qa_lead: ROLES.QA_LEAD,
  admin: ROLES.ADMIN,
};

interface AccessTokenClaims {
  sub?: string;
  preferred_username?: string;
  realm_access?: { roles?: string[] };
}

/** Giải mã base64url payload của JWT → claims. Token dị dạng → null (không throw). */
export function decodeJwtPayload(token: string): AccessTokenClaims | null {
  const parts = token.split('.');
  if (parts.length < 2) return null;
  try {
    const b64 = parts[1].replace(/-/g, '+').replace(/_/g, '/');
    const pad = b64.length % 4 ? '='.repeat(4 - (b64.length % 4)) : '';
    const bin = atob(b64 + pad);
    const bytes = Uint8Array.from(bin, (c) => c.charCodeAt(0));
    return JSON.parse(new TextDecoder().decode(bytes)) as AccessTokenClaims;
  } catch {
    return null;
  }
}

/** realm_access.roles (Keycloak) → app Role[] (map qua RoleMap, dedupe, bỏ role không khớp). */
export function rolesFromToken(token: string): Role[] {
  const kcRoles = decodeJwtPayload(token)?.realm_access?.roles ?? [];
  const mapped = kcRoles
    .map((r) => KEYCLOAK_ROLE_MAP[r?.toLowerCase?.()])
    .filter((r): r is Role => Boolean(r));
  return [...new Set(mapped)];
}

/** Định danh hiển thị: preferred_username → sub (Keycloak sub là UUID, không thân thiện). */
export function subjectFromToken(token: string): string {
  const claims = decodeJwtPayload(token);
  return claims?.preferred_username || claims?.sub || 'unknown';
}
