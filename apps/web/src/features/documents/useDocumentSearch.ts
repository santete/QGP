import { useCallback, useEffect, useState } from 'react';
import { ApiError, searchDocuments } from '../../api/client';
import type { SearchHit } from '../../api/types';

/**
 * Tìm kiếm tài liệu (S3) qua /v1/search (Meilisearch full-text, chỉ Effective — BR-06).
 * q rỗng → toàn bộ tài liệu Effective (ranked). Trả kèm snippet + score.
 */
export function useDocumentSearch(q: string) {
  const [items, setItems] = useState<SearchHit[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<ApiError | null>(null);

  const load = useCallback(
    (signal?: AbortSignal) => {
      setLoading(true);
      setError(null);
      searchDocuments(q || undefined, {}, signal)
        .then((rows) => {
          if (!signal?.aborted) setItems(rows);
        })
        .catch((err: unknown) => {
          if (signal?.aborted) return;
          setError(err instanceof ApiError ? err : new ApiError(0, 'NETWORK_ERROR', 'Không kết nối được máy chủ'));
        })
        .finally(() => {
          if (!signal?.aborted) setLoading(false);
        });
    },
    [q],
  );

  useEffect(() => {
    const ctrl = new AbortController();
    load(ctrl.signal);
    return () => ctrl.abort();
  }, [load]);

  return { items, loading, error };
}
