import type { ReactNode } from 'react';
import { Navigate, Outlet, useLocation } from 'react-router-dom';
import { useAuth } from './AuthContext';
import type { Role } from './roles';

/** Chặn route khi chưa đăng nhập → chuyển /login (nhớ nơi định đến). */
export function RequireAuth({ children }: { children?: ReactNode }) {
  const { isAuthenticated } = useAuth();
  const location = useLocation();
  if (!isAuthenticated) {
    return <Navigate to="/login" replace state={{ from: location.pathname }} />;
  }
  return children ? <>{children}</> : <Outlet />;
}

/** Chặn route khi thiếu role (default deny) → chuyển /403. Server vẫn enforce lại (RBAC §10.1). */
export function RequireRole({ anyOf, children }: { anyOf: Role[]; children?: ReactNode }) {
  const { hasAnyRole } = useAuth();
  if (!hasAnyRole(anyOf)) {
    return <Navigate to="/403" replace />;
  }
  return children ? <>{children}</> : <Outlet />;
}
