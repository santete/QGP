import { useEffect, useState } from 'react';
import { Card } from '../components/Card';
import { Badge } from '../components/Badge';
import { Skeleton } from '../components/Skeleton';
import { EmptyState } from '../components/EmptyState';
import { ErrorState } from '../components/ErrorState';
import { ApiError, listAudit } from '../api/client';
import type { AuditEntry } from '../api/types';
import { strings } from '../i18n/strings';

function formatAt(iso?: string): string {
  if (!iso) return '';
  const d = new Date(iso);
  return Number.isNaN(d.getTime())
    ? iso
    : d.toLocaleString('vi-VN', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' });
}

/** Màn Nhật ký kiểm toán (S16, ADM-F-03): bảng audit bất biến. RBAC admin.config. */
export function AuditPage() {
  const t = strings.audit;
  const [items, setItems] = useState<AuditEntry[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<ApiError | null>(null);

  useEffect(() => {
    const ctrl = new AbortController();
    setLoading(true);
    setError(null);
    listAudit({ limit: 50 }, ctrl.signal)
      .then((rows) => {
        if (!ctrl.signal.aborted) setItems(rows);
      })
      .catch((err: unknown) => {
        if (ctrl.signal.aborted) return;
        setError(err instanceof ApiError ? err : new ApiError(0, 'NETWORK_ERROR', 'Không kết nối được máy chủ'));
      })
      .finally(() => {
        if (!ctrl.signal.aborted) setLoading(false);
      });
    return () => ctrl.abort();
  }, []);

  return (
    <div className="mx-auto w-full max-w-4xl px-4 py-6 md:py-8">
      <h1 className="text-2xl font-bold text-text-primary">{t.title}</h1>
      <p className="mb-4 mt-1 text-sm text-text-secondary">{t.subtitle}</p>

      {loading ? (
        <Skeleton className="h-64 w-full" />
      ) : error ? (
        <Card className="p-4">
          <ErrorState message={error.message} code={error.code} />
        </Card>
      ) : items.length === 0 ? (
        <EmptyState title={t.empty} />
      ) : (
        <>
          <p className="mb-2 text-xs text-text-secondary">{t.count(items.length)}</p>
          <Card className="overflow-x-auto p-0">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-dashed border-divider text-left text-xs uppercase tracking-wide text-text-secondary">
                  <th className="px-4 py-2.5 font-semibold">{t.col_time}</th>
                  <th className="px-4 py-2.5 font-semibold">{t.col_actor}</th>
                  <th className="px-4 py-2.5 font-semibold">{t.col_action}</th>
                </tr>
              </thead>
              <tbody>
                {items.map((e) => (
                  <tr key={e.id} className="border-b border-dashed border-divider/60 last:border-0">
                    <td className="whitespace-nowrap px-4 py-2.5 text-text-secondary">{formatAt(e.at)}</td>
                    <td className="px-4 py-2.5">
                      {e.actor === 'system' ? (
                        <Badge color="default">system</Badge>
                      ) : (
                        <span className="font-medium text-text-primary">{e.actor}</span>
                      )}
                    </td>
                    <td className="px-4 py-2.5 font-mono text-xs text-text-primary">{e.action}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </Card>
        </>
      )}
    </div>
  );
}
