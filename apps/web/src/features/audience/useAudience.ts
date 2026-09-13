import { useCallback, useEffect, useState } from 'react';
import { ApiError, getDocAudience, setDocAudience } from '../../api/client';
import type { DocAudienceItem } from '../../api/types';

interface AudienceState {
  audience: DocAudienceItem[];
  loading: boolean;
  error: ApiError | null;
}

const INITIAL: AudienceState = {
  audience: [],
  loading: true,
  error: null,
};

/**
 * Hook tải/sửa audience roles của 1 tài liệu (ADM-F-02).
 * Gọi getDocAudience on mount (AbortController), save gọi setDocAudience.
 */
export function useAudience(docId: string) {
  const [state, setState] = useState<AudienceState>(INITIAL);

  const load = useCallback(
    (signal?: AbortSignal) => {
      setState((s) => ({ ...s, loading: true, error: null }));
      getDocAudience(docId, signal)
        .then((audience) => {
          if (signal?.aborted) return;
          setState({ audience, loading: false, error: null });
        })
        .catch((err: unknown) => {
          if (signal?.aborted) return;
          const apiErr =
            err instanceof ApiError
              ? err
              : new ApiError(0, 'NETWORK_ERROR', 'Không kết nối được máy chủ');
          setState({ ...INITIAL, loading: false, error: apiErr });
        });
    },
    [docId],
  );

  useEffect(() => {
    const ctrl = new AbortController();
    load(ctrl.signal);
    return () => ctrl.abort();
  }, [load]);

  const save = useCallback(
    async (audience: DocAudienceItem[]): Promise<DocAudienceItem[]> => {
      const result = await setDocAudience(docId, audience);
      setState({ audience: result, loading: false, error: null });
      return result;
    },
    [docId],
  );

  return { ...state, save };
}
