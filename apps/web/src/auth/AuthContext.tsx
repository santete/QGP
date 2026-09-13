import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from 'react';
import { useAuth as useOidcAuth } from 'react-oidc-context';
import type { Role } from './roles';
import { OIDC_ENABLED } from './oidcConfig';
import { rolesFromToken, subjectFromToken } from './tokenRoles';

const USE_MOCK = import.meta.env.VITE_USE_MOCK === '1';
const TOKEN_KEY = 'qgp.access_token';
const USER_KEY = 'qgp.user';

export interface AuthUser {
  sub: string;
  roles: Role[];
}

interface AuthApi {
  user: AuthUser | null;
  isAuthenticated: boolean;
  login: (sub: string, roles: Role[]) => Promise<void>;
  logout: () => void;
  hasAnyRole: (roles: Role[]) => boolean;
}

const AuthContext = createContext<AuthApi | null>(null);

function loadUser(): AuthUser | null {
  try {
    const raw = localStorage.getItem(USER_KEY);
    return raw ? (JSON.parse(raw) as AuthUser) : null;
  } catch {
    return null;
  }
}

/**
 * Provider chọn theo cấu hình: OIDC thật (Keycloak, A1b) khi VITE_OIDC_AUTHORITY có,
 * ngược lại dùng mock/dev-login (Q1). Giữ nguyên surface useAuth() cho guards/pages.
 */
export function AuthProvider({ children }: { children: ReactNode }) {
  return OIDC_ENABLED ? (
    <OidcAuthProvider>{children}</OidcAuthProvider>
  ) : (
    <LocalAuthProvider>{children}</LocalAuthProvider>
  );
}

/** Chế độ mock/dev-login (không IdP): phiên cục bộ trong localStorage. */
function LocalAuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(() => loadUser());

  const login = useCallback(async (sub: string, roles: Role[]) => {
    // Dev mock (Q1): tạo phiên cục bộ, không cần IdP. Real: gọi BE /auth/dev-login.
    const token = USE_MOCK ? `mock.${sub}.${roles.join('-')}` : await realDevLogin(sub, roles);
    const u: AuthUser = { sub, roles };
    localStorage.setItem(TOKEN_KEY, token);
    localStorage.setItem(USER_KEY, JSON.stringify(u));
    setUser(u);
  }, []);

  const logout = useCallback(() => {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
    setUser(null);
  }, []);

  const hasAnyRole = useCallback(
    (roles: Role[]) => !!user && roles.some((r) => user.roles.includes(r)),
    [user],
  );

  const api = useMemo<AuthApi>(
    () => ({ user, isAuthenticated: !!user, login, logout, hasAnyRole }),
    [user, login, logout, hasAnyRole],
  );

  return <AuthContext.Provider value={api}>{children}</AuthContext.Provider>;
}

/**
 * Chế độ OIDC thật — adapt react-oidc-context sang surface useAuth() của app.
 * PHẢI nằm trong <AuthProvider> của react-oidc-context (bọc ở App khi OIDC_ENABLED).
 * User + roles suy từ access token Keycloak; token được sync vào localStorage cho client.ts.
 */
export function OidcAuthProvider({ children }: { children: ReactNode }) {
  const oidc = useOidcAuth();
  const accessToken = oidc.user?.access_token ?? null;

  const user = useMemo<AuthUser | null>(
    () => (accessToken ? { sub: subjectFromToken(accessToken), roles: rolesFromToken(accessToken) } : null),
    [accessToken],
  );

  // Đồng bộ Bearer token cho client.ts (nguồn: localStorage 'qgp.access_token').
  useEffect(() => {
    if (accessToken) localStorage.setItem(TOKEN_KEY, accessToken);
    else localStorage.removeItem(TOKEN_KEY);
  }, [accessToken]);

  const login = useCallback(async () => {
    await oidc.signinRedirect();
  }, [oidc]);

  const logout = useCallback(() => {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
    void oidc.signoutRedirect();
  }, [oidc]);

  const hasAnyRole = useCallback(
    (roles: Role[]) => !!user && roles.some((r) => user.roles.includes(r)),
    [user],
  );

  const api = useMemo<AuthApi>(
    () => ({ user, isAuthenticated: !!user, login, logout, hasAnyRole }),
    [user, login, logout, hasAnyRole],
  );

  // Chờ lib xử lý xong (kể cả callback ?code) để tránh nháy /login; báo lỗi nếu IdP hỏng.
  if (oidc.isLoading) return <AuthSplash message="Đang kết nối SSO…" />;
  if (oidc.error) {
    return (
      <AuthSplash message={`Đăng nhập SSO thất bại: ${oidc.error.message}`}>
        <button
          type="button"
          onClick={() => void oidc.signinRedirect()}
          className="mt-4 rounded-md bg-primary px-4 py-2 text-sm font-bold text-white"
        >
          Thử lại đăng nhập
        </button>
      </AuthSplash>
    );
  }

  return <AuthContext.Provider value={api}>{children}</AuthContext.Provider>;
}

/** Splash tối giản cho trạng thái loading/error của OIDC (tránh nháy route khi callback). */
function AuthSplash({ message, children }: { message: string; children?: ReactNode }) {
  return (
    <div className="grid min-h-screen place-items-center px-4 text-center">
      <div>
        <p className="text-sm text-text-secondary">{message}</p>
        {children}
      </div>
    </div>
  );
}

// eslint-disable-next-line react-refresh/only-export-components
export function useAuth(): AuthApi {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth phải nằm trong <AuthProvider>');
  return ctx;
}

/** Gọi BE dev-login thật (khi không mock) — trả access_token. Endpoint ở /auth (không dưới /v1). */
async function realDevLogin(sub: string, roles: Role[]): Promise<string> {
  const res = await fetch('/auth/dev-login', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ sub, roles }),
  });
  if (!res.ok) throw new Error('dev-login thất bại');
  const data = (await res.json()) as { access_token: string };
  return data.access_token;
}
