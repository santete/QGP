import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { Card } from '../components/Card';
import { Badge } from '../components/Badge';
import { Skeleton } from '../components/Skeleton';
import { EmptyState } from '../components/EmptyState';
import { ErrorState } from '../components/ErrorState';
import { useToast } from '../components/Toast';
import { ApiError, listFeedback, triageFeedback } from '../api/client';
import type { Feedback, FeedbackStatus } from '../api/types';
import { strings } from '../i18n/strings';

const STATUSES: FeedbackStatus[] = ['New', 'Triaged', 'InProgress', 'Resolved', 'Rejected'];

/** Màn Xử lý phản hồi (S14, FBK-F-03): list + triage đổi trạng thái. RBAC admin.config. */
export function FeedbackTriagePage() {
  const t = strings.feedback;
  const toast = useToast();
  const [items, setItems] = useState<Feedback[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<ApiError | null>(null);
  const [busyId, setBusyId] = useState<string | null>(null);

  useEffect(() => {
    const ctrl = new AbortController();
    setLoading(true);
    setError(null);
    listFeedback({ limit: 100 }, ctrl.signal)
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

  async function changeStatus(id: string, status: FeedbackStatus) {
    setBusyId(id);
    try {
      const updated = await triageFeedback(id, status);
      setItems((prev) => prev.map((f) => (f.id === id ? updated : f)));
      toast.show(t.updated, 'success');
    } catch (err) {
      toast.show(err instanceof ApiError ? err.message : 'Lỗi', 'error');
    } finally {
      setBusyId(null);
    }
  }

  return (
    <div className="mx-auto w-full max-w-3xl px-4 py-6 md:py-8">
      <h1 className="text-2xl font-bold text-text-primary">{t.triage_title}</h1>
      <p className="mb-4 mt-1 text-sm text-text-secondary">{t.triage_subtitle}</p>

      {loading ? (
        <div className="space-y-3">
          <Skeleton className="h-24 w-full" />
          <Skeleton className="h-24 w-full" />
        </div>
      ) : error ? (
        <Card className="p-4">
          <ErrorState message={error.message} code={error.code} />
        </Card>
      ) : items.length === 0 ? (
        <EmptyState title={t.empty} />
      ) : (
        <>
          <p className="mb-2 text-xs text-text-secondary">{t.count(items.length)}</p>
          <ul className="space-y-3">
            {items.map((f) => (
              <li key={f.id}>
                <Card className="p-4">
                  <div className="flex flex-wrap items-center gap-2">
                    {f.doc_id && (
                      <Link
                        to={`/documents/${f.doc_id}`}
                        className="text-xs font-bold uppercase tracking-wide text-primary hover:underline"
                      >
                        {f.doc_id}
                      </Link>
                    )}
                    {f.version && <Badge>v{f.version}</Badge>}
                    <Badge color="warning">{t.cat[f.category ?? 'question']}</Badge>
                  </div>
                  {f.body && <p className="mt-2 text-sm text-text-primary">{f.body}</p>}
                  <div className="mt-3 flex items-center gap-2">
                    <span className="text-xs text-text-secondary">{t.status[f.status ?? 'New']}</span>
                    <select
                      value={f.status ?? 'New'}
                      disabled={busyId === f.id}
                      onChange={(e) => void changeStatus(f.id!, e.target.value as FeedbackStatus)}
                      aria-label={`status-${f.id}`}
                      className="ml-auto rounded-md border border-divider bg-bg-paper px-2 py-1.5 text-sm outline-none focus:border-primary"
                    >
                      {STATUSES.map((s) => (
                        <option key={s} value={s}>
                          {t.status[s]}
                        </option>
                      ))}
                    </select>
                  </div>
                </Card>
              </li>
            ))}
          </ul>
        </>
      )}
    </div>
  );
}
