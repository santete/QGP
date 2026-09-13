import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { OidcAuthProvider, useAuth } from '../auth/AuthContext';
import { ROLES } from '../auth/roles';

// Mock react-oidc-context: OidcAuthProvider tiêu thụ useAuth() của lib.
const signinRedirect = vi.fn();
const signoutRedirect = vi.fn();
let mockOidc: {
  isLoading: boolean;
  error: Error | null;
  user: { access_token: string } | null;
  signinRedirect: typeof signinRedirect;
  signoutRedirect: typeof signoutRedirect;
};

vi.mock('react-oidc-context', () => ({
  useAuth: () => mockOidc,
}));

function makeJwt(payload: object): string {
  const b64url = (obj: object) =>
    btoa(JSON.stringify(obj)).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
  return `${b64url({ alg: 'RS256' })}.${b64url(payload)}.sig`;
}

function Probe() {
  const { user, isAuthenticated, login, logout, hasAnyRole } = useAuth();
  return (
    <div>
      <span data-testid="auth">{isAuthenticated ? 'yes' : 'no'}</span>
      <span data-testid="sub">{user?.sub ?? '-'}</span>
      <span data-testid="admin">{hasAnyRole([ROLES.ADMIN]) ? 'admin' : 'plain'}</span>
      <span data-testid="author">{hasAnyRole([ROLES.AUTHOR]) ? 'author' : 'no'}</span>
      <button onClick={() => void login('ignored', [])}>login</button>
      <button onClick={logout}>logout</button>
    </div>
  );
}

beforeEach(() => {
  localStorage.clear();
  signinRedirect.mockReset();
  signoutRedirect.mockReset();
  mockOidc = { isLoading: false, error: null, user: null, signinRedirect, signoutRedirect };
});
afterEach(() => localStorage.clear());

describe('OidcAuthProvider', () => {
  it('token Keycloak → user.sub (preferred_username) + roles + sync qgp.access_token', () => {
    const token = makeJwt({
      sub: 'kc-uuid',
      preferred_username: 'author1',
      realm_access: { roles: ['author', 'reader'] },
    });
    mockOidc.user = { access_token: token };

    render(
      <OidcAuthProvider>
        <Probe />
      </OidcAuthProvider>,
    );

    expect(screen.getByTestId('auth').textContent).toBe('yes');
    expect(screen.getByTestId('sub').textContent).toBe('author1');
    expect(screen.getByTestId('author').textContent).toBe('author');
    expect(screen.getByTestId('admin').textContent).toBe('plain');
    expect(localStorage.getItem('qgp.access_token')).toBe(token);
  });

  it('chưa đăng nhập → not authenticated + không có token', () => {
    render(
      <OidcAuthProvider>
        <Probe />
      </OidcAuthProvider>,
    );
    expect(screen.getByTestId('auth').textContent).toBe('no');
    expect(localStorage.getItem('qgp.access_token')).toBeNull();
  });

  it('login() gọi signinRedirect', () => {
    render(
      <OidcAuthProvider>
        <Probe />
      </OidcAuthProvider>,
    );
    fireEvent.click(screen.getByText('login'));
    expect(signinRedirect).toHaveBeenCalledTimes(1);
  });

  it('logout() xoá token + gọi signoutRedirect', () => {
    mockOidc.user = { access_token: makeJwt({ preferred_username: 'a', realm_access: { roles: ['reader'] } }) };
    render(
      <OidcAuthProvider>
        <Probe />
      </OidcAuthProvider>,
    );
    expect(localStorage.getItem('qgp.access_token')).not.toBeNull();
    fireEvent.click(screen.getByText('logout'));
    expect(signoutRedirect).toHaveBeenCalledTimes(1);
    expect(localStorage.getItem('qgp.access_token')).toBeNull();
  });

  it('isLoading → hiển thị splash, chưa render children', () => {
    mockOidc.isLoading = true;
    render(
      <OidcAuthProvider>
        <Probe />
      </OidcAuthProvider>,
    );
    expect(screen.queryByTestId('auth')).not.toBeInTheDocument();
  });

  it('error → hiển thị lỗi + nút thử lại gọi signinRedirect', async () => {
    mockOidc.error = new Error('oidc boom');
    render(
      <OidcAuthProvider>
        <Probe />
      </OidcAuthProvider>,
    );
    expect(screen.queryByTestId('auth')).not.toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: /thử lại|đăng nhập/i }));
    await waitFor(() => expect(signinRedirect).toHaveBeenCalled());
  });
});
