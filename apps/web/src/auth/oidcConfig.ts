import type { AuthProviderProps } from 'react-oidc-context';

/**
 * Cấu hình OIDC (A1b) — Authorization Code + PKCE tới Keycloak (client public qgp-web).
 * Bật OIDC khi có VITE_OIDC_AUTHORITY; không có → giữ nguyên mock/dev-login (backward-compatible).
 */
const authority = import.meta.env.VITE_OIDC_AUTHORITY as string | undefined;

export const OIDC_ENABLED = Boolean(authority);

const origin = typeof window !== 'undefined' ? window.location.origin : 'http://localhost:5173';

export const oidcConfig: AuthProviderProps = {
  authority: authority ?? '',
  client_id: (import.meta.env.VITE_OIDC_CLIENT_ID as string) || 'qgp-web',
  redirect_uri: `${origin}/`,
  post_logout_redirect_uri: `${origin}/`,
  scope: 'openid profile email',
  // Access token 15' → tự làm mới bằng refresh token (public client PKCE) để không rớt phiên giữa chừng.
  automaticSilentRenew: true,
  // response_type 'code' + PKCE là mặc định của oidc-client-ts (public client).
  // Dọn ?code&state khỏi URL sau khi lib xử lý callback.
  onSigninCallback: () => {
    if (typeof window !== 'undefined') {
      window.history.replaceState({}, document.title, window.location.pathname);
    }
  },
};
