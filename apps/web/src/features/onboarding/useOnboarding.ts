import { useEffect, useState } from 'react';
import { ApiError, getOnboarding } from '../../api/client';
import type { Onboarding } from '../../api/types';

interface OnboardingState {
  data: Onboarding | null;
  loading: boolean;
  error: ApiError | null;
}

/** Nạp lộ trình onboarding theo role (S12, ONB-F-01/02/04). */
export function useOnboarding(): OnboardingState {
  const [data, setData] = useState<Onboarding | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<ApiError | null>(null);

  useEffect(() => {
    const ctrl = new AbortController();
    setLoading(true);
    setError(null);
    getOnboarding(ctrl.signal)
      .then((d) => {
        if (!ctrl.signal.aborted) setData(d);
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
