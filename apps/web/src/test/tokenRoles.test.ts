import { describe, expect, it } from 'vitest';
import { decodeJwtPayload, rolesFromToken, subjectFromToken } from '../auth/tokenRoles';
import { ROLES } from '../auth/roles';

/** Tạo JWT giả (chỉ payload có nghĩa) — base64url, không ký thật (FE chỉ đọc claim cho UX). */
function makeJwt(payload: object): string {
  const b64url = (obj: object) =>
    btoa(JSON.stringify(obj)).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
  return `${b64url({ alg: 'RS256', typ: 'JWT' })}.${b64url(payload)}.sig`;
}

describe('tokenRoles', () => {
  it('map realm_access.roles (lowercase Keycloak) → app Role, dedupe + bỏ role lạ', () => {
    const token = makeJwt({
      sub: 'kc-uuid',
      realm_access: { roles: ['author', 'reader', 'offline_access', 'author'] },
    });
    const roles = rolesFromToken(token);
    expect(roles).toContain(ROLES.AUTHOR);
    expect(roles).toContain(ROLES.READER);
    expect(roles).not.toContain('offline_access' as never);
    expect(roles.filter((r) => r === ROLES.AUTHOR)).toHaveLength(1); // dedupe
  });

  it('không có realm_access → [] ', () => {
    expect(rolesFromToken(makeJwt({ sub: 'x' }))).toEqual([]);
  });

  it('subjectFromToken ưu tiên preferred_username, fallback sub', () => {
    expect(subjectFromToken(makeJwt({ sub: 'uuid-1', preferred_username: 'author1' }))).toBe('author1');
    expect(subjectFromToken(makeJwt({ sub: 'uuid-2' }))).toBe('uuid-2');
  });

  it('decodeJwtPayload trả null với token rác', () => {
    expect(decodeJwtPayload('not-a-jwt')).toBeNull();
    expect(decodeJwtPayload('a.b')).toBeNull(); // payload 'b' không phải JSON hợp lệ
  });

  it('map case-insensitive với tên role Keycloak', () => {
    const roles = rolesFromToken(makeJwt({ realm_access: { roles: ['ADMIN', 'Qa_Lead'] } }));
    expect(roles).toContain(ROLES.ADMIN);
    expect(roles).toContain(ROLES.QA_LEAD);
  });
});
