import { Link, useParams } from 'react-router-dom';
import { Card } from '../components/Card';
import { Badge } from '../components/Badge';
import { StatusBadge } from '../components/StatusBadge';
import { Skeleton } from '../components/Skeleton';
import { EmptyState } from '../components/EmptyState';
import { ErrorState } from '../components/ErrorState';
import { useVersionHistory } from '../features/documents/useVersionHistory';
import { strings } from '../i18n/strings';

/** Màn Lịch sử phiên bản (S6, DOC-F-06): chọn 2 version → xem unified diff. */
export function HistoryPage() {
  const { docId } = useParams<{ docId: string }>();
  if (!docId) return <EmptyState title="Thiếu mã tài liệu" />;
  return <History docId={docId} />;
}

function History({ docId }: { docId: string }) {
  const { versions, from, setFrom, to, setTo, diff, loading, diffLoading, error } = useVersionHistory(docId);
  const t = strings.history;

  return (
    <div className="mx-auto w-full max-w-3xl px-4 py-6 md:py-8">
      <div className="mb-1 flex items-center justify-between gap-2">
        <h1 className="text-2xl font-bold text-text-primary">{t.title}</h1>
        <Link to={`/documents/${docId}`} className="text-sm font-semibold text-primary hover:underline">
          {t.back_to_doc}
        </Link>
      </div>
      <p className="mb-4 text-sm text-text-secondary">
        {docId} · {t.subtitle}
      </p>

      {loading ? (
        <Skeleton className="h-40 w-full" />
      ) : error ? (
        <Card className="p-4">
          <ErrorState message={error.message} code={error.code} />
        </Card>
      ) : versions.length < 2 ? (
        <EmptyState title={t.pick_two} />
      ) : (
        <>
          {/* Danh sách phiên bản */}
          <Card className="mb-4 p-4">
            <ul className="space-y-2">
              {versions.map((v) => (
                <li key={v.id} className="flex flex-wrap items-center gap-2 text-sm">
                  <Badge>v{v.version}</Badge>
                  <StatusBadge status={v.status} />
                  {v.change_summary && <span className="text-text-secondary">— {v.change_summary}</span>}
                </li>
              ))}
            </ul>
          </Card>

          {/* Bộ chọn 2 mốc */}
          <div className="mb-4 flex flex-wrap items-end gap-3">
            <label className="text-sm">
              <span className="mb-1 block font-semibold text-text-primary">{t.from}</span>
              <select
                value={from}
                onChange={(e) => setFrom(e.target.value)}
                aria-label={t.from}
                className="rounded-md border border-divider bg-bg-paper px-3 py-2 text-sm outline-none focus:border-primary"
              >
                {versions.map((v) => (
                  <option key={v.id} value={v.version}>
                    v{v.version}
                  </option>
                ))}
              </select>
            </label>
            <label className="text-sm">
              <span className="mb-1 block font-semibold text-text-primary">{t.to}</span>
              <select
                value={to}
                onChange={(e) => setTo(e.target.value)}
                aria-label={t.to}
                className="rounded-md border border-divider bg-bg-paper px-3 py-2 text-sm outline-none focus:border-primary"
              >
                {versions.map((v) => (
                  <option key={v.id} value={v.version}>
                    v{v.version}
                  </option>
                ))}
              </select>
            </label>
          </div>

          {/* Diff */}
          {diffLoading ? (
            <Skeleton className="h-40 w-full" />
          ) : diff && diff.diff.trim() === '' ? (
            <EmptyState title={t.no_diff} />
          ) : diff ? (
            <Card className="overflow-hidden p-0">
              {diff.change_summary && (
                <div className="border-b border-divider px-4 py-2 text-sm text-text-secondary">
                  {t.change_summary}: {diff.change_summary}
                </div>
              )}
              <DiffView diff={diff.diff} />
            </Card>
          ) : null}
        </>
      )}
    </div>
  );
}

/** Render unified diff: tô màu dòng thêm (+) / xoá (-) / header. */
function DiffView({ diff }: { diff: string }) {
  const lines = diff.replace(/\n$/, '').split('\n');
  return (
    <pre className="overflow-x-auto p-0 text-xs leading-relaxed">
      {lines.map((line, i) => {
        const cls =
          line.startsWith('+') && !line.startsWith('+++')
            ? 'bg-success/10 text-success-darker'
            : line.startsWith('-') && !line.startsWith('---')
              ? 'bg-error/10 text-error-darker'
              : line.startsWith('@@')
                ? 'bg-primary-lighter/40 text-primary-darker'
                : line.startsWith('+++') || line.startsWith('---')
                  ? 'text-text-disabled'
                  : 'text-text-secondary';
        return (
          <div key={i} className={`whitespace-pre px-4 ${cls}`}>
            {line === '' ? ' ' : line}
          </div>
        );
      })}
    </pre>
  );
}
