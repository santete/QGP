import { NavLink } from 'react-router-dom';
import { cn } from '../lib/cn';
import { useAuth } from '../auth/AuthContext';
import { ADMIN_ROLES, APPROVE_ROLES, AUTHOR_ROLES, type Role } from '../auth/roles';
import { strings } from '../i18n/strings';

interface NavItem {
  to: string;
  label: string;
  roles?: Role[]; // undefined = mọi user đã đăng nhập
  end?: boolean; // khớp chính xác (dùng cho '/')
}

/** Điều hướng role-aware (§8 AppNav) — ẩn mục ngoài quyền (§7) + đánh dấu mục đang mở (active). */
export function AppNav() {
  const { hasAnyRole } = useAuth();

  const items: NavItem[] = [
    { to: '/', label: strings.home.open, end: true },
    { to: '/start', label: strings.start.open },
    { to: '/onboarding', label: strings.onboarding.open },
    { to: '/documents', label: strings.docs.title },
    { to: '/kb', label: strings.kb.title },
    { to: '/editor/new', label: strings.editor.new_doc, roles: AUTHOR_ROLES },
    { to: '/review', label: strings.review.title, roles: APPROVE_ROLES },
    { to: '/publish', label: strings.publish.title, roles: APPROVE_ROLES },
    { to: '/feedback', label: strings.feedback.triage_title, roles: ADMIN_ROLES },
    { to: '/reports', label: strings.reports.open, roles: ADMIN_ROLES },
    { to: '/audit', label: strings.audit.title, roles: ADMIN_ROLES },
    { to: '/admin', label: 'Quản trị', roles: ADMIN_ROLES },
  ];

  return (
    <>
      {items
        .filter((it) => !it.roles || hasAnyRole(it.roles))
        .map((it) => (
          <NavLink
            key={it.to}
            to={it.to}
            end={it.end}
            className={({ isActive }) =>
              cn(
                'rounded-md px-2.5 py-1.5 text-sm font-semibold transition-colors',
                isActive
                  ? 'bg-primary-lighter text-primary-darker'
                  : 'text-text-secondary hover:bg-primary-lighter/40 hover:text-primary',
              )
            }
          >
            {it.label}
          </NavLink>
        ))}
    </>
  );
}
