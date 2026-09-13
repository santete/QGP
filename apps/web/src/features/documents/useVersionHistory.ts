import { useEffect, useState } from 'react';
import { ApiError, getDiff, listVersions } from '../../api/client';
import type { DiffResult, DocumentVersion } from '../../api/types';

/**
 * Lịch sử phiên bản (S6): nạp danh sách version + diff giữa 2 bản chọn.
 * Mặc định so bản mới nhất với bản liền trước.
 */
export function useVersionHistory(docId: string) {
  const [versions, setVersions] = useState<DocumentVersion[]>([]);
  const [from, setFrom] = useState<string>('');
  const [to, setTo] = useState<string>('');
  const [diff, setDiff] = useState<DiffResult | null>(null);
  const [loading, setLoading] = useState(true);
  const [diffLoading, setDiffLoading] = useState(false);
  const [error, setError] = useState<ApiError | null>(null);

  // Nạp danh sách version 1 lần.
  useEffect(() => {
    const ctrl = new AbortController();
    setLoading(true);
    setError(null);
    listVersions(docId, ctrl.signal)
      .then((vs) => {
        if (ctrl.signal.aborted) return;
        setVersions(vs);
        // vs sắp xếp mới → cũ. Mặc định: from = bản liền trước, to = bản mới nhất.
        if (vs.length >= 2) {
          setFrom(vs[1].version);
          setTo(vs[0].version);
        } else if (vs.length === 1) {
          setFrom(vs[0].version);
          setTo(vs[0].version);
        }
      })
      .catch((err: unknown) => {
        if (ctrl.signal.aborted) return;
        setError(err instanceof ApiError ? err : new ApiError(0, 'NETWORK_ERROR', 'Không kết nối được máy chủ'));
      })
      .finally(() => {
        if (!ctrl.signal.aborted) setLoading(false);
      });
    return () => ctrl.abort();
  }, [docId]);

  // Nạp diff mỗi khi from/to đổi (và có đủ 2 mốc).
  useEffect(() => {
    if (!from || !to) return;
    const ctrl = new AbortController();
    setDiffLoading(true);
    getDiff(docId, from, to, ctrl.signal)
      .then((d) => {
        if (!ctrl.signal.aborted) setDiff(d);
      })
      .catch((err: unknown) => {
        if (ctrl.signal.aborted) return;
        setError(err instanceof ApiError ? err : new ApiError(0, 'NETWORK_ERROR', 'Không kết nối được máy chủ'));
      })
      .finally(() => {
        if (!ctrl.signal.aborted) setDiffLoading(false);
      });
    return () => ctrl.abort();
  }, [docId, from, to]);

  return { versions, from, setFrom, to, setTo, diff, loading, diffLoading, error };
}
