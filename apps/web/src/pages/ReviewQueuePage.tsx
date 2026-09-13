import { useState } from 'react';
import { Link } from 'react-router-dom';
import { Card } from '../components/Card';
import { Badge } from '../components/Badge';
import { Button } from '../components/Button';
import { Skeleton } from '../components/Skeleton';
import { EmptyState } from '../components/EmptyState';
import { ErrorState } from '../components/ErrorState';
import { useToast } from '../components/Toast';
import { ApiError } from '../api/client';
import { useReviewQueue } from '../features/review/useReviewQueue';
import { strings } from '../i18n/strings';

/** Hàng đợi duyệt (S9): danh sách version InReview + duyệt/từ chối. RBAC doc.approve. */
export function ReviewQueuePage() {
  const { items, loading, error, decide } = useReviewQueue();
  const toast = useToast();

  // Item đang mở ô nhập lý do từ chối + nội dung comment + id đang xử lý.
  const [rejectingId, setRejectingId] = useState<string | null>(null);
  const [comment, setComment] = useState('');
  const [busyId, setBusyId] = useState<string | null>(null);

  const t = strings.review;

  async function run(versionId: string, decision: 'approve' | 'reject', text?: string) {
    setBusyId(versionId);
    try {
      await decide(versionId, decision, text);
      toast.show(decision === 'approve' ? t.approved_toast : t.rejected_toast, 'success');
      setRejectingId(null);
      setComment('');
    } catch (err) {
      const msg = err instanceof ApiError ? err.message : t.action_failed;
      toast.show(msg, 'error');
    } finally {
      setBusyId(null);
    }
  }

  function onReject(versionId: string) {
    if (rejectingId !== versionId) {
      setRejectingId(versionId);
      setComment('');
      return;
    }
    if (!comment.trim()) {
      toast.show(t.reject_comment_required, 'error');
      return;
    }
    void run(versionId, 'reject', comment.trim());
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
              const busy = busyId === it.version_id;
              const rejecting = rejectingId === it.version_id;
              return (
                <li key={it.version_id}>
                  <Card className="p-4">
                    <div className="flex flex-wrap items-center gap-2">
                      <span className="text-xs font-bold uppercase tracking-wide text-text-secondary">
                        {it.doc_id}
                      </span>
                      <Badge>v{it.version}</Badge>
                    </div>
                    <h2 className="mt-1.5 font-semibold text-text-primary">{it.title}</h2>
                    <p className="mt-1 text-xs text-text-secondary">
                      {t.submitted_at}: {formatDate(it.submitted_at)}
                    </p>

                    {rejecting && (
                      <textarea
                        value={comment}
                        onChange={(e) => setComment(e.target.value)}
                        placeholder={t.reject_comment_placeholder}
                        aria-label={t.reject_comment_placeholder}
                        rows={2}
                        className="mt-3 w-full rounded-md border border-divider bg-bg-paper px-3 py-2 text-sm outline-none focus:border-primary"
                      />
                    )}

                    <div className="mt-3 flex flex-wrap items-center gap-2">
                      <Button
                        size="sm"
                        loading={busy && !rejecting}
                        disabled={busy}
                        onClick={() => void run(it.version_id, 'approve')}
                      >
                        {t.approve}
                      </Button>
                      <Button
                        size="sm"
                        variant="outlined"
                        loading={busy && rejecting}
                        disabled={busy}
                        onClick={() => onReject(it.version_id)}
                      >
                        {t.reject}
                      </Button>
                      <Link
                        to={`/documents/${it.doc_id}`}
                        className="ml-auto text-sm font-semibold text-primary hover:underline"
                      >
                        {t.view}
                      </Link>
                    </div>
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

/** Ngày giờ gửi duyệt → chuỗi ngắn dd/MM/yyyy HH:mm (vi-VN). */
function formatDate(iso: string): string {
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) return iso;
  return d.toLocaleString('vi-VN', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
}
