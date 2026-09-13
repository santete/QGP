import { useEffect, useState } from 'react';
import { listSubscriptions, subscribeDocument, unsubscribeDocument } from '../../api/client';
import { Button } from '../../components/Button';
import { strings } from '../../i18n/strings';

/** Toggle theo dõi tài liệu (S19, ADM-F-04) — nhận thông báo khi có bản Effective mới. */
export function SubscribeButton({ docId }: { docId: string }) {
  const t = strings.subscriptions;
  const [subscribed, setSubscribed] = useState<boolean | null>(null); // null = đang tải
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    const ctrl = new AbortController();
    listSubscriptions(ctrl.signal)
      .then((subs) => {
        if (!ctrl.signal.aborted) setSubscribed(subs.some((s) => s.doc_id === docId));
      })
      .catch(() => {
        if (!ctrl.signal.aborted) setSubscribed(false);
      });
    return () => ctrl.abort();
  }, [docId]);

  async function toggle() {
    if (subscribed === null || busy) return;
    setBusy(true);
    try {
      const res = subscribed ? await unsubscribeDocument(docId) : await subscribeDocument(docId);
      setSubscribed(res.subscribed);
    } catch {
      /* giữ nguyên trạng thái nếu lỗi */
    } finally {
      setBusy(false);
    }
  }

  const isOn = subscribed === true;
  return (
    <Button
      variant={isOn ? 'contained' : 'outlined'}
      size="sm"
      onClick={toggle}
      disabled={subscribed === null || busy}
      title={t.hint}
      aria-pressed={isOn}
    >
      {isOn ? `🔔 ${t.following}` : `➕ ${t.follow}`}
    </Button>
  );
}
