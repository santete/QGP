import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { Card } from '../components/Card';
import { Badge } from '../components/Badge';
import { StatusBadge } from '../components/StatusBadge';
import { Skeleton } from '../components/Skeleton';
import { EmptyState } from '../components/EmptyState';
import { ErrorState } from '../components/ErrorState';
import { useDocumentSearch } from '../features/documents/useDocumentSearch';
import { strings } from '../i18n/strings';

/** Màn Tài liệu (S3): ô tìm kiếm full-text (/v1/search, chỉ Effective — BR-06) + snippet. */
export function DocumentsPage() {
  const [input, setInput] = useState('');
  const [q, setQ] = useState('');

  // Debounce 300ms để không gọi API mỗi phím.
  useEffect(() => {
    const t = setTimeout(() => setQ(input), 300);
    return () => clearTimeout(t);
  }, [input]);

  const { items, loading, error } = useDocumentSearch(q);

  return (
    <div className="mx-auto w-full max-w-3xl px-4 py-6 md:py-8">
      <h1 className="mb-4 text-2xl font-bold text-text-primary">{strings.docs.title}</h1>

      <input
        type="search"
        value={input}
        onChange={(e) => setInput(e.target.value)}
        placeholder={strings.docs.search_placeholder}
        aria-label={strings.docs.search_placeholder}
        className="mb-4 w-full rounded-md border border-divider bg-bg-paper px-4 py-2.5 text-sm outline-none focus:border-primary"
      />

      {loading ? (
        <div className="space-y-3">
          <Skeleton className="h-20 w-full" />
          <Skeleton className="h-20 w-full" />
        </div>
      ) : error ? (
        <Card className="p-4">
          <ErrorState message={error.message} code={error.code} />
        </Card>
      ) : items.length === 0 ? (
        <EmptyState title={strings.docs.empty} />
      ) : (
        <>
          <p className="mb-2 text-xs text-text-secondary">{strings.docs.count(items.length)}</p>
          <ul className="space-y-3">
            {items.map((d) => (
              <li key={d.doc_id}>
                <Link to={`/documents/${d.doc_id}`} className="block">
                  <Card className="p-4 transition-shadow hover:shadow-dropdown">
                    <div className="flex flex-wrap items-center gap-2">
                      <span className="text-xs font-bold uppercase tracking-wide text-text-secondary">
                        {d.doc_id}
                      </span>
                      {/* Search chỉ index bản Effective (BR-06) → luôn hiển thị trạng thái áp dụng. */}
                      <StatusBadge status="Effective" />
                      {d.version && <Badge>v{d.version}</Badge>}
                    </div>
                    <h2 className="mt-1.5 font-semibold text-text-primary">{d.title}</h2>
                    {d.snippet && (
                      <p className="mt-1.5 line-clamp-2 text-sm text-text-secondary">{d.snippet}</p>
                    )}
                  </Card>
                </Link>
              </li>
            ))}
          </ul>
        </>
      )}
    </div>
  );
}
