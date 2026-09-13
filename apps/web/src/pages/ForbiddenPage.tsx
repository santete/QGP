import { useNavigate } from 'react-router-dom';
import { EmptyState } from '../components/EmptyState';
import { Button } from '../components/Button';
import { strings } from '../i18n/strings';

/** Trang 403 — thiếu quyền (route guard RequireRole chuyển tới đây). */
export function ForbiddenPage() {
  const navigate = useNavigate();
  return (
    <div className="mx-auto w-full max-w-2xl px-4 py-10">
      <EmptyState
        icon="🔒"
        title={strings.auth.forbidden_title}
        description={strings.auth.forbidden_desc}
        action={
          <Button variant="soft" size="sm" onClick={() => navigate('/')}>
            {strings.auth.back_home}
          </Button>
        }
      />
    </div>
  );
}
