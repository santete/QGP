import { useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { Card } from '../components/Card';
import { Badge } from '../components/Badge';
import { StatusBadge } from '../components/StatusBadge';
import { Skeleton } from '../components/Skeleton';
import { EmptyState } from '../components/EmptyState';
import { ErrorState } from '../components/ErrorState';
import { ApiError, listDocuments } from '../api/client';
import type { DocumentSummary } from '../api/types';
import { strings } from '../i18n/strings';

/** Màn Kho tri thức (S10, KB-F-03): browse tài liệu Effective nhóm theo loại (dùng lại listDocuments, BR-06). */
export function KbPage() {
  const t = strings.kb;
  const [docs, setDocs] = useState<DocumentSummary[]>([]);
  const [q, setQ] = useState('');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<ApiError | null>(null);

  useEffect(() => {
    const ctrl = new AbortController();
    setLoading(true);
    setError(null);
    listDocuments(undefined, ctrl.signal)
      .then((rows) => {
        if (!ctrl.signal.aborted) setDocs(rows.filter((d) => d.status === 'Effective'));
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

  // Lọc client + nhóm theo type.
  const groups = useMemo(() => {
    const term = q.trim().toLowerCase();
    const filtered = term
      ? docs.filter((d) => d.title.toLowerCase().includes(term) || d.doc_id.toLowerCase().includes(term))
      : docs;
    const map = new Map<string, DocumentSummary[]>();
    for (const d of filtered) {
      const arr = map.get(d.type) ?? [];
      arr.push(d);
      map.set(d.type, arr);
    }
    return [...map.entries()].sort((a, b) => a[0].localeCompare(b[0]));
  }, [docs, q]);

  const total = useMemo(() => groups.reduce((n, [, arr]) => n + arr.length, 0), [groups]);

  return (
    <div className="mx-auto w-full max-w-3xl px-4 py-6 md:py-8">
      <h1 className="text-2xl font-bold text-text-primary">{t.title}</h1>
      <p className="mb-4 mt-1 text-sm text-text-secondary">{t.subtitle}</p>

      {loading ? (
        <div className="space-y-3">
          <Skeleton className="h-20 w-full" />
          <Skeleton className="h-20 w-full" />
        </div>
      ) : error ? (
        <Card className="p-4">
          <ErrorState message={error.message} code={error.code} />
        </Card>
      ) : docs.length === 0 ? (
        <EmptyState title={t.empty} />
      ) : (
        <>
          <input
            type="search"
            value={q}
            onChange={(e) => setQ(e.target.value)}
            placeholder={t.search_ph}
            aria-label={t.search_ph}
            className="mb-4 w-full rounded-md border border-divider bg-bg-paper px-4 py-2.5 text-sm outline-none focus:border-primary"
          />
          <p className="mb-3 text-xs text-text-secondary">{t.count(total)}</p>
          <div className="space-y-6">
            {groups.map(([type, items]) => (
              <section key={type}>
                <h2 className="mb-2 flex items-center gap-2 text-sm font-bold uppercase tracking-wide text-text-secondary">
                  {type} <Badge color="default">{items.length}</Badge>
                </h2>
                <ul className="space-y-2">
                  {items.map((d) => (
                    <li key={d.doc_id}>
                      <Link to={`/documents/${d.doc_id}`} className="block">
                        <Card className="p-3 transition-shadow hover:shadow-dropdown">
                          <div className="flex flex-wrap items-center gap-2">
                            <span className="text-xs font-bold uppercase tracking-wide text-text-secondary">{d.doc_id}</span>
                            <StatusBadge status="Effective" />
                            {d.effective_version && <Badge>v{d.effective_version}</Badge>}
                          </div>
                          <h3 className="mt-1 font-semibold text-text-primary">{d.title}</h3>
                        </Card>
                      </Link>
                    </li>
                  ))}
                </ul>
              </section>
            ))}
          </div>
        </>
      )}
    </div>
  );
}
