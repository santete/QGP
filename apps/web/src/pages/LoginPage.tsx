import { useState } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { Card } from '../components/Card';
import { Button } from '../components/Button';
import { useAuth } from '../auth/AuthContext';
import { OIDC_ENABLED } from '../auth/oidcConfig';
import { ALL_ROLES, ROLE_LABEL, ROLES, type Role } from '../auth/roles';
import { strings } from '../i18n/strings';

const USE_MOCK = import.meta.env.VITE_USE_MOCK === '1';

/** Màn đăng nhập S1 ("Chào mừng trở lại"). Prod: nút SSO. Dev: chọn nhanh vai trò (mock OIDC). */
export function LoginPage() {
  const { login } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const from = (location.state as { from?: string } | null)?.from ?? '/';

  const [name, setName] = useState('nguoi.dung@fpt');
  const [roles, setRoles] = useState<Role[]>([ROLES.READER]);
  const [loading, setLoading] = useState(false);

  function toggleRole(r: Role) {
    setRoles((prev) => (prev.includes(r) ? prev.filter((x) => x !== r) : [...prev, r]));
  }

  async function submit(selectedRoles: Role[]) {
    // OIDC: nút SSO redirect sang Keycloak (login bỏ qua sub/roles); callback quay lại rồi mới điều hướng.
    if (OIDC_ENABLED) {
      setLoading(true);
      await login('', []);
      return;
    }
    if (!name.trim() || selectedRoles.length === 0) return;
    setLoading(true);
    try {
      await login(name.trim(), selectedRoles);
      navigate(from, { replace: true });
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="mx-auto flex min-h-[70vh] w-full max-w-md items-center px-4">
      <Card className="w-full p-8">
        <div className="mb-6 flex items-center gap-2">
          <span className="grid h-9 w-9 place-items-center rounded-md bg-primary text-white">Q</span>
          <span className="text-lg font-extrabold text-text-primary">{strings.app.short}</span>
        </div>

        <h1 className="text-2xl font-bold text-text-primary">{strings.auth.login_title}</h1>
        <p className="mt-1 text-sm text-text-secondary">{strings.auth.login_subtitle}</p>

        <Button
          className="mt-6 w-full"
          onClick={() => submit(roles)}
          loading={loading}
          disabled={!name.trim() || roles.length === 0}
        >
          {strings.auth.sso_button}
        </Button>

        {USE_MOCK && (
          <div className="mt-6 border-t border-dashed border-divider pt-5">
            <label className="block text-xs font-semibold text-text-secondary" htmlFor="sub">
              {strings.auth.name_label}
            </label>
            <input
              id="sub"
              value={name}
              onChange={(e) => setName(e.target.value)}
              className="mt-1 w-full rounded-md border border-divider bg-bg-default px-3 py-2 text-sm outline-none focus:border-primary"
            />
            <p className="mt-4 text-xs text-text-secondary">{strings.auth.dev_hint}</p>
            <div className="mt-2 flex flex-wrap gap-2">
              {ALL_ROLES.map((r) => (
                <button
                  key={r}
                  type="button"
                  aria-pressed={roles.includes(r)}
                  onClick={() => toggleRole(r)}
                  className={
                    'rounded-md px-2.5 py-1 text-xs font-bold transition-colors ' +
                    (roles.includes(r)
                      ? 'bg-primary text-white'
                      : 'bg-bg-neutral text-text-secondary hover:bg-primary-lighter')
                  }
                >
                  {ROLE_LABEL[r]}
                </button>
              ))}
            </div>
          </div>
        )}
      </Card>
    </div>
  );
}
