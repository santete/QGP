import { useState } from 'react';
import { Button } from '../../components/Button';
import { useToast } from '../../components/Toast';
import { ApiError, createFeedback, getDocument } from '../../api/client';
import type { FeedbackCategory } from '../../api/types';
import { strings } from '../../i18n/strings';

const CATS: FeedbackCategory[] = ['content_error', 'unclear', 'improvement', 'question'];

/** S13 — gửi phản hồi in-context trên tài liệu đang đọc (FBK-F-01). Server tự gắn version. */
export function FeedbackButton({ docId }: { docId: string }) {
  const t = strings.feedback;
  const toast = useToast();
  const [open, setOpen] = useState(false);
  const [category, setCategory] = useState<FeedbackCategory>('content_error');
  const [body, setBody] = useState('');
  const [busy, setBusy] = useState(false);

  async function send() {
    if (!body.trim()) {
      toast.show(t.required, 'error');
      return;
    }
    setBusy(true);
    try {
      const doc = await getDocument(docId);
      await createFeedback({ doc_id: docId, version: doc.effective_version ?? '', category, body: body.trim() });
      toast.show(t.sent, 'success');
      setOpen(false);
      setBody('');
    } catch (err) {
      toast.show(err instanceof ApiError ? err.message : t.failed, 'error');
    } finally {
      setBusy(false);
    }
  }

  if (!open) {
    return (
      <Button variant="outlined" size="sm" onClick={() => setOpen(true)}>
        💬 {t.open}
      </Button>
    );
  }

  return (
    <div className="rounded-md border border-divider bg-bg-paper p-3 shadow-dropdown">
      <div className="mb-2 text-sm font-bold text-text-primary">{t.title}</div>
      <label className="mb-2 block text-sm">
        <span className="mb-1 block text-text-secondary">{t.category}</span>
        <select
          value={category}
          onChange={(e) => setCategory(e.target.value as FeedbackCategory)}
          aria-label={t.category}
          className="w-full rounded-md border border-divider bg-bg-paper px-3 py-2 text-sm outline-none focus:border-primary"
        >
          {CATS.map((c) => (
            <option key={c} value={c}>
              {t.cat[c]}
            </option>
          ))}
        </select>
      </label>
      <textarea
        value={body}
        onChange={(e) => setBody(e.target.value)}
        placeholder={t.body_ph}
        aria-label={t.body}
        rows={3}
        className="w-full rounded-md border border-divider bg-bg-paper px-3 py-2 text-sm outline-none focus:border-primary"
      />
      <div className="mt-2 flex gap-2">
        <Button size="sm" loading={busy} disabled={busy} onClick={() => void send()}>
          {t.send}
        </Button>
        <Button size="sm" variant="text" disabled={busy} onClick={() => setOpen(false)}>
          {t.cancel}
        </Button>
      </div>
    </div>
  );
}
