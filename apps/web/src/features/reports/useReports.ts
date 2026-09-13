import { useEffect, useState } from 'react';
import {
  ApiError,
  getAccessReport,
  getComplianceReport,
  getFeedbackReport,
  getIssuanceReport,
} from '../../api/client';
import type { AccessReport, ComplianceReport, FeedbackReport, IssuanceReport } from '../../api/types';

interface ReportsState {
  issuance: IssuanceReport | null;
  compliance: ComplianceReport | null;
  feedback: FeedbackReport | null;
  access: AccessReport | null;
  loading: boolean;
  error: ApiError | null;
}

/** Nạp 4 báo cáo quản trị song song (S15, RPT-F-01/02/03/04/05). */
export function useReports(): ReportsState {
  const [issuance, setIssuance] = useState<IssuanceReport | null>(null);
  const [compliance, setCompliance] = useState<ComplianceReport | null>(null);
  const [feedback, setFeedback] = useState<FeedbackReport | null>(null);
  const [access, setAccess] = useState<AccessReport | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<ApiError | null>(null);

  useEffect(() => {
    const ctrl = new AbortController();
    setLoading(true);
    setError(null);
    Promise.all([
      getIssuanceReport(ctrl.signal),
      getComplianceReport(ctrl.signal),
      getFeedbackReport(ctrl.signal),
      getAccessReport(ctrl.signal),
    ])
      .then(([iss, comp, fb, acc]) => {
        if (ctrl.signal.aborted) return;
        setIssuance(iss);
        setCompliance(comp);
        setFeedback(fb);
        setAccess(acc);
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

  return { issuance, compliance, feedback, access, loading, error };
}
