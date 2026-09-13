import { useEffect, useState } from 'react';
import { Button } from '../../components/Button';
import { Card } from '../../components/Card';
import { useToast } from '../../components/Toast';
import { ALL_ROLES, ROLE_LABEL, type Role } from '../../auth/roles';
import { strings } from '../../i18n/strings';
import { useAudience } from './useAudience';

/**
 * Panel sửa audience roles per-document (ADM-F-02).
 * Hiển thị khi user có role QA_LEAD/ADMIN (ADMIN_ROLES).
 * Gọi getDocAudience để load, setDocAudience để save (PUT thay thế toàn bộ).
 */
export function AudiencePanel({ docId }: { docId: string }) {
  const t = strings.audience;
  const toast = useToast();
  const { audience, loading, error, save } = useAudience(docId);

  // Local draft state — chỉnh trước khi bấm Lưu.
  const [draft, setDraft] = useState<{ role: string; reason: string }[]>([]);
  const [dirty, setDirty] = useState(false);
  const [saving, setSaving] = useState(false);

  // Sync draft khi audience load xong (chỉ 1 lần, khi chưa dirty).
  useEffect(() => {
    if (!loading && !dirty && audience.length > 0) {
      setDraft(audience.map((a) => ({ role: a.role, reason: a.reason ?? '' })));
    }
  }, [loading, dirty, audience]);

  const availableRoles = ALL_ROLES.filter((r) => !draft.some((d) => d.role === r));

  function addRole(role: Role) {
    setDraft((prev) => [...prev, { role, reason: '' }]);
    setDirty(true);
  }

  function removeRole(role: string) {
    setDraft((prev) => prev.filter((d) => d.role !== role));
    setDirty(true);
  }

  function updateReason(role: string, reason: string) {
    setDraft((prev) => prev.map((d) => (d.role === role ? { ...d, reason } : d)));
    setDirty(true);
  }

  async function handleSave() {
    if (saving) return;
    setSaving(true);
    try {
      const result = await save(draft.map((d) => ({ role: d.role, reason: d.reason || null })));
      setDraft(result.map((a) => ({ role: a.role, reason: a.reason ?? '' })));
      setDirty(false);
      toast.show(t.saved, 'success');
    } catch {
      toast.show(t.failed, 'error');
    } finally {
      setSaving(false);
    }
  }

  if (loading) {
    return (
      <Card className="p-4">
        <p className="text-sm text-text-secondary">{t.loading}</p>
      </Card>
    );
  }

  if (error) {
    return (
      <Card className="p-4">
        <p className="text-sm text-error">{strings.common.error_title}</p>
        <p className="mt-1 text-xs text-text-secondary">{error.message}</p>
      </Card>
    );
  }

  return (
    <Card className="p-4">
      <h2 className="text-lg font-bold text-text-primary">{t.title}</h2>
      <p className="mt-1 text-sm text-text-secondary">{t.subtitle}</p>
      <p className="mt-2 text-xs text-warning">{t.replace_warning}</p>

      {draft.length === 0 && !dirty ? (
        <p className="mt-3 text-sm text-text-secondary">{t.empty}</p>
      ) : (
        <ul className="mt-3 space-y-2">
          {draft.map((item) => (
            <li key={item.role} className="flex items-center gap-2 rounded-md border border-divider bg-bg-paper px-3 py-2">
              <span className="text-sm font-semibold text-text-primary">
                {ROLE_LABEL[item.role as Role] ?? item.role}
              </span>
              <input
                type="text"
                value={item.reason}
                onChange={(e) => updateReason(item.role, e.target.value)}
                placeholder={t.reason_placeholder}
                aria-label={t.reason_label}
                className="flex-1 rounded-md border border-divider bg-bg-paper px-2 py-1 text-sm outline-none focus:border-primary"
              />
              <button
                type="button"
                onClick={() => removeRole(item.role)}
                className="text-sm font-semibold text-error hover:underline"
                aria-label={`${t.remove} ${item.role}`}
              >
                {t.remove}
              </button>
            </li>
          ))}
        </ul>
      )}

      {/* Add role dropdown — chỉ role chưa có trong draft */}
      {availableRoles.length > 0 && (
        <div className="mt-3 flex items-center gap-2">
          <select
            aria-label={t.add}
            defaultValue=""
            onChange={(e) => {
              if (e.target.value) {
                addRole(e.target.value as Role);
                e.target.value = '';
              }
            }}
            className="rounded-md border border-divider bg-bg-paper px-3 py-1.5 text-sm outline-none focus:border-primary"
          >
            <option value="" disabled>
              {t.add}…
            </option>
            {availableRoles.map((r) => (
              <option key={r} value={r}>
                {ROLE_LABEL[r]}
              </option>
            ))}
          </select>
        </div>
      )}

      <div className="mt-4 flex justify-end gap-2">
        <Button
          size="sm"
          loading={saving}
          disabled={saving}
          onClick={() => void handleSave()}
        >
          {t.save}
        </Button>
      </div>
    </Card>
  );
}
