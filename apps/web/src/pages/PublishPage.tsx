import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { Card } from '../components/Card';
import { Badge } from '../components/Badge';
import { Button } from '../components/Button';
import { Skeleton } from '../components/Skeleton';
import { EmptyState } from '../components/EmptyState';
import { ErrorState } from '../components/ErrorState';
import { useToast } from '../components/Toast';
import { ApiError, listReviewQueue, publishVersion } from '../api/client';
import type { ReviewQueueItem } from '../api/types';
import { strings } from '../i18n/strings';

function todayISO(): string {
  return new Date().toISOString().slice(0, 10);
}

/** Màn Chờ ban hành (S8, DOC-F-03/UC-03): list bản Approved + form ban hành (BR-04/07). RBAC doc.approve. */
export function PublishPage() {
  const t = strings.publish;
  const toast = useToast();
  const [items, setItems] = useState<ReviewQueueItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<ApiError | null>(null);
  const [openId, setOpenId] = useState<string | null>(null);
  const [busyId, setBusyId] = useState<string | null>(null);

  // Form state (per item đang mở).
  const [issue, setIssue] = useState(todayISO());
  const [effective, setEffective] = useState(todayISO());
  const [summary, setSummary] = useState('');

  function load() {
    setLoading(true);
    setError(null);
    listReviewQueue('Approved')
      .then(setItems)
      .catch((err: unknown) =>
        setError(err instanceof ApiError ? err : new ApiError(0, 'NETWORK_ERROR', 'Không kết nối được máy chủ')),
      )
      .finally(() => setLoading(false));
  }

  useEffect(load, []);

  function openForm(id: string) {
    setOpenId(id);
    setIssue(todayISO());
    setEffective(todayISO());
    setSummary('');
  }

  async function confirmPublish(id: string) {
    if (!issue || !effective || !summary.trim()) {
      toast.show(t.required, 'error');
      return;
    }
    if (effective < issue) {
      toast.show(t.eff_before_issue, 'error');
      return;
    }
    setBusyId(id);
    try {
      await publishVersion(id, { issue_date: issue, effective_date: effective, change_summary: summary.trim() });
      toast.show(t.published_toast, 'success');
      setItems((prev) => prev.filter((x) => x.version_id !== id));
      setOpenId(null);
    } catch (err) {
      toast.show(err instanceof ApiError ? err.message : t.failed, 'error');
    } finally {
      setBusyId(null);
    }
  }

  return (
    <div className="mx-auto w-full max-w-3xl px-4 py-6 md:py-8">
      <h1 className="text-2xl font-bold text-text-primary">{t.title}</h1>
      <p className="mb-4 mt-1 text-sm text-text-secondary">{t.subtitle}</p>

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
            {items.map((it) => {
              const open = openId === it.version_id;
              const busy = busyId === it.version_id;
              return (
                <li key={it.version_id}>
                  <Card className="p-4">
                    <div className="flex flex-wrap items-center gap-2">
                      <span className="text-xs font-bold uppercase tracking-wide text-text-secondary">{it.doc_id}</span>
                      <Badge>v{it.version}</Badge>
                    </div>
                    <h2 className="mt-1.5 font-semibold text-text-primary">{it.title}</h2>

                    {open ? (
                      <div className="mt-3 space-y-3">
                        <div className="flex flex-wrap gap-3">
                          <label className="text-sm">
                            <span className="mb-1 block font-semibold text-text-primary">{t.issue_date}</span>
                            <input
                              type="date"
                              value={issue}
                              onChange={(e) => setIssue(e.target.value)}
                              aria-label={t.issue_date}
                              className="rounded-md border border-divider bg-bg-paper px-3 py-2 text-sm outline-none focus:border-primary"
                            />
                          </label>
                          <label className="text-sm">
                            <span className="mb-1 block font-semibold text-text-primary">{t.effective_date}</span>
                            <input
                              type="date"
                              value={effective}
                              onChange={(e) => setEffective(e.target.value)}
                              aria-label={t.effective_date}
                              className="rounded-md border border-divider bg-bg-paper px-3 py-2 text-sm outline-none focus:border-primary"
                            />
                          </label>
                        </div>
                        <textarea
                          value={summary}
                          onChange={(e) => setSummary(e.target.value)}
                          placeholder={t.change_summary_ph}
                          aria-label={t.change_summary}
                          rows={2}
                          className="w-full rounded-md border border-divider bg-bg-paper px-3 py-2 text-sm outline-none focus:border-primary"
                        />
                        <div className="flex gap-2">
                          <Button size="sm" loading={busy} disabled={busy} onClick={() => void confirmPublish(it.version_id)}>
                            {t.submit}
                          </Button>
                          <Button size="sm" variant="text" disabled={busy} onClick={() => setOpenId(null)}>
                            {t.cancel}
                          </Button>
                        </div>
                      </div>
                    ) : (
                      <div className="mt-3 flex items-center gap-2">
                        <Button size="sm" onClick={() => openForm(it.version_id)}>
                          {t.open}
                        </Button>
                        <Link
                          to={`/documents/${it.doc_id}/history`}
                          className="ml-auto text-sm font-semibold text-primary hover:underline"
                        >
                          {strings.history.open}
                        </Link>
                      </div>
                    )}
                  </Card>
                </li>
              );
            })}
          </ul>
        </>
      )}
    </div>
  );
}
