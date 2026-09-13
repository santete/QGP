import { useCallback, useEffect, useState } from 'react';
import { ApiError, getDocument, listVersions } from '../../api/client';
import type { DocumentDetail, DocumentVersion } from '../../api/types';

interface DocumentState {
  data: DocumentDetail | null;
  versions: DocumentVersion[];
  /** UUID của bản Effective — cần cho acknowledge (DocumentDetail không cấp id). */
  effectiveVersionId: string | null;
  loading: boolean;
  error: ApiError | null;
}

const INITIAL: DocumentState = {
  data: null,
  versions: [],
  effectiveVersionId: null,
  loading: true,
  error: null,
};

/** Chọn bản áp dụng: version === effective_version, fallback status Effective. */
function pickEffective(doc: DocumentDetail, versions: DocumentVersion[]): DocumentVersion | undefined {
  return (
    versions.find((v) => doc.effective_version != null && v.version === doc.effective_version) ??
    versions.find((v) => v.status === 'Effective')
  );
}

export function useDocument(docId: string) {
  const [state, setState] = useState<DocumentState>(INITIAL);

  const load = useCallback(
    (signal?: AbortSignal) => {
      setState((s) => ({ ...s, loading: true, error: null }));
      Promise.all([getDocument(docId, signal), listVersions(docId, signal)])
        .then(([data, versions]) => {
          if (signal?.aborted) return;
          const eff = pickEffective(data, versions);
          setState({
            data,
            versions,
            effectiveVersionId: eff?.id ?? null,
            loading: false,
            error: null,
          });
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

  const reload = useCallback(() => load(), [load]);

  return { ...state, reload };
}
