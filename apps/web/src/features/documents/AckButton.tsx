import { useState } from 'react';
import { ApiError, acknowledgeVersion } from '../../api/client';
import { Button } from '../../components/Button';
import { useToast } from '../../components/Toast';
import { strings } from '../../i18n/strings';

/**
 * Xác nhận đã đọc tài liệu bắt buộc (DOC-F-09, UC-10).
 * Chỉ hiện khi mandatory_ack=true và có versionId của bản Effective.
 */
export function AckButton({ versionId }: { versionId: string }) {
  const toast = useToast();
  const [acked, setAcked] = useState(false);
  const [loading, setLoading] = useState(false);

  async function handleAck() {
    setLoading(true);
    try {
      await acknowledgeVersion(versionId);
      setAcked(true);
      toast.show(strings.doc.ack_done, 'success');
    } catch (err) {
      const msg = err instanceof ApiError ? err.message : strings.doc.ack_failed;
      toast.show(msg, 'error');
    } finally {
      setLoading(false);
    }
  }

  if (acked) {
    return (
      <div className="flex items-center gap-2 rounded-lg bg-success-lighter px-4 py-3 text-sm font-semibold text-success-darker">
        <span aria-hidden="true">✓</span>
        {strings.doc.ack_done}
      </div>
    );
  }

  return (
    <div className="flex flex-wrap items-center justify-between gap-3 rounded-lg bg-primary-lighter/60 px-4 py-3">
      <p className="text-sm font-medium text-primary-darker">{strings.doc.ack_required}</p>
      <Button onClick={handleAck} loading={loading} size="sm">
        {strings.doc.ack_button}
      </Button>
    </div>
  );
}
