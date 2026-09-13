import { useCallback, useEffect, useState } from 'react';
import { ApiError, approveVersion, listReviewQueue } from '../../api/client';
import type { ReviewQueueItem } from '../../api/types';

/** Nạp hàng đợi duyệt (S9) + hành động duyệt/từ chối (gỡ item khỏi hàng đợi khi xong). */
export function useReviewQueue() {
  const [items, setItems] = useState<ReviewQueueItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<ApiError | null>(null);

  const load = useCallback((signal?: AbortSignal) => {
    setLoading(true);
    setError(null);
    listReviewQueue('InReview', signal)
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
  }, []);

  useEffect(() => {
    const ctrl = new AbortController();
    load(ctrl.signal);
    return () => ctrl.abort();
  }, [load]);

  /** Duyệt (approve) hoặc từ chối (reject, bắt buộc comment). Thành công → gỡ khỏi hàng đợi. */
  const decide = useCallback(
    async (versionId: string, decision: 'approve' | 'reject', comment?: string) => {
      await approveVersion(versionId, decision, comment);
      setItems((prev) => prev.filter((x) => x.version_id !== versionId));
    },
    [],
  );

  return { items, loading, error, decide };
}
