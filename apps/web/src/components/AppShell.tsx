import type { ReactNode } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { strings } from '../i18n/strings';
import { useAuth } from '../auth/AuthContext';
import { NotificationBell } from '../features/notifications/NotificationBell';
import { AppNav } from './AppNav';
import { Button } from './Button';

/** Khung ứng dụng tối giản (header 64/80). Điều hướng role-aware qua AppNav (§8) + bell + logout. */
export function AppShell({ children }: { children: ReactNode }) {
  const { user, logout } = useAuth();
  const navigate = useNavigate();

  function handleLogout() {
    logout();
    navigate('/login', { replace: true });
  }

  return (
    <div className="min-h-full">
      <header className="sticky top-0 z-40 flex h-16 items-center justify-between border-b border-dashed border-divider bg-bg-paper/80 px-4 backdrop-blur md:h-20 md:px-8">
        <Link to="/" className="flex items-center gap-2" title={strings.app.name} aria-label={strings.app.name}>
          <span className="grid h-8 w-8 place-items-center rounded-md bg-primary text-base font-extrabold text-white">Q</span>
          <span className="text-lg font-extrabold tracking-tight text-text-primary">{strings.app.short}</span>
        </Link>

        {user && (
          <div className="flex items-center gap-3">
            <nav className="flex items-center gap-3" aria-label="Điều hướng chính">
              <AppNav />
            </nav>
            <NotificationBell />
            <span className="hidden text-sm text-text-secondary md:inline">{user.sub}</span>
            <Button variant="outlined" size="sm" onClick={handleLogout}>
              {strings.auth.logout}
            </Button>
          </div>
        )}
      </header>
      <main>{children}</main>
    </div>
  );
}
