import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { AuthProvider, useAuth, type AuthUser } from '../auth/AuthContext';
import { RequireAuth, RequireRole } from '../auth/guards';
import { ROLES, ADMIN_ROLES } from '../auth/roles';

beforeEach(() => localStorage.clear());
afterEach(() => {
  localStorage.clear();
  vi.unstubAllGlobals();
});

function seedUser(u: AuthUser) {
  localStorage.setItem('qgp.user', JSON.stringify(u));
}

function Probe() {
  const { user, isAuthenticated, login, logout, hasAnyRole } = useAuth();
  return (
    <div>
      <span data-testid="auth">{isAuthenticated ? 'yes' : 'no'}</span>
      <span data-testid="sub">{user?.sub ?? '-'}</span>
      <span data-testid="admin">{hasAnyRole([ROLES.ADMIN]) ? 'admin' : 'plain'}</span>
      <button onClick={() => void login('carol@fpt', [ROLES.ADMIN])}>login</button>
      <button onClick={logout}>logout</button>
    </div>
  );
}

describe('AuthContext', () => {
  it('login lưu phiên + hasAnyRole, logout xoá phiên', async () => {
    // Test mode không có VITE_USE_MOCK → login gọi BE dev-login: stub fetch.
    vi.stubGlobal(
      'fetch',
      vi.fn(async () => ({ ok: true, json: async () => ({ access_token: 'tok' }) })),
    );

    render(
      <AuthProvider>
        <Probe />
      </AuthProvider>,
    );

    expect(screen.getByTestId('auth').textContent).toBe('no');
    fireEvent.click(screen.getByText('login'));

    await waitFor(() => expect(screen.getByTestId('auth').textContent).toBe('yes'));
    expect(screen.getByTestId('sub').textContent).toBe('carol@fpt');
    expect(screen.getByTestId('admin').textContent).toBe('admin');
    expect(localStorage.getItem('qgp.access_token')).toBe('tok');

    fireEvent.click(screen.getByText('logout'));
    await waitFor(() => expect(screen.getByTestId('auth').textContent).toBe('no'));
    expect(localStorage.getItem('qgp.access_token')).toBeNull();
  });
});

describe('route guards', () => {
  function renderAt(path: string) {
    return render(
      <AuthProvider>
        <MemoryRouter initialEntries={[path]}>
          <Routes>
            <Route path="/login" element={<div>LOGIN</div>} />
            <Route path="/403" element={<div>FORBIDDEN</div>} />
            <Route
              path="/secret"
              element={
                <RequireAuth>
                  <div>SECRET</div>
                </RequireAuth>
              }
            />
            <Route
              path="/admin"
              element={
                <RequireRole anyOf={ADMIN_ROLES}>
                  <div>ADMIN</div>
                </RequireRole>
              }
            />
          </Routes>
        </MemoryRouter>
      </AuthProvider>,
    );
  }

  it('RequireAuth chuyển /login khi chưa đăng nhập', () => {
    renderAt('/secret');
    expect(screen.getByText('LOGIN')).toBeInTheDocument();
    expect(screen.queryByText('SECRET')).not.toBeInTheDocument();
  });

  it('RequireRole chặn user thiếu quyền → /403', () => {
    seedUser({ sub: 'bob', roles: [ROLES.READER] });
    renderAt('/admin');
    expect(screen.getByText('FORBIDDEN')).toBeInTheDocument();
  });

  it('RequireRole cho qua khi đủ quyền', () => {
    seedUser({ sub: 'carol', roles: [ROLES.ADMIN] });
    renderAt('/admin');
    expect(screen.getByText('ADMIN')).toBeInTheDocument();
  });
});
