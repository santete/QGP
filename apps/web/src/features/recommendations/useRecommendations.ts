import { useEffect, useState } from 'react';
import { ApiError, getRecommendations } from '../../api/client';
import type { Recommendations } from '../../api/types';

interface RecommendationsState {
  data: Recommendations | null;
  loading: boolean;
  error: ApiError | null;
}

/** Nạp gợi ý REC (S2/S11, BR-11) — dùng chung cho Home và "Bắt đầu từ đâu". */
export function useRecommendations(): RecommendationsState {
  const [data, setData] = useState<Recommendations | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<ApiError | null>(null);

  useEffect(() => {
    const ctrl = new AbortController();
    setLoading(true);
    setError(null);
    getRecommendations(ctrl.signal)
      .then((recs) => {
        if (!ctrl.signal.aborted) setData(recs);
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

  return { data, loading, error };
}
