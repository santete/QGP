import { useEffect, useState, type ReactNode } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { Card } from '../components/Card';
import { Button } from '../components/Button';
import { Skeleton } from '../components/Skeleton';
import { ErrorState } from '../components/ErrorState';
import { useToast } from '../components/Toast';
import {
  ApiError,
  createDocument,
  getVersionContent,
  listVersions,
  submitVersion,
  updateVersionContent,
} from '../api/client';
import type { DocumentType } from '../api/types';
import { strings } from '../i18n/strings';

const DOC_TYPES: DocumentType[] = [
  'Policy',
  'Process',
  'Procedure',
  'Work Instruction',
  'Template',
  'Checklist',
  'Standard',
];

/** Editor S7: soạn tài liệu mới (/editor/new) hoặc sửa bản nháp (/editor/:versionId). RBAC doc.author. */
export function EditorPage() {
  const { versionId } = useParams();
  const isNew = !versionId;
  return isNew ? <NewDocumentForm /> : <EditDraft versionId={versionId!} />;
}

/** Tạo tài liệu mới (Draft v1.0) rồi chuyển sang màn sửa. */
function NewDocumentForm() {
  const navigate = useNavigate();
  const toast = useToast();
  const t = strings.editor;

  const [docId, setDocId] = useState('');
  const [title, setTitle] = useState('');
  const [type, setType] = useState<DocumentType>('Process');
  const [content, setContent] = useState('');
  const [busy, setBusy] = useState(false);

  async function onCreate() {
    if (!docId.trim() || !title.trim() || !content.trim()) {
      toast.show(t.required, 'error');
      return;
    }
    setBusy(true);
    try {
      const doc = await createDocument({
        doc_id: docId.trim(),
        title: title.trim(),
        type,
        mandatory_ack: false,
        content_markdown: content,
      });
      // POST /documents không trả version id → lấy bản Draft v1.0 qua listVersions.
      const versions = await listVersions(doc.doc_id);
      const draft = versions.find((v) => v.status === 'Draft') ?? versions[0];
      toast.show(t.created_toast, 'success');
      navigate(`/editor/${draft.id}`, { replace: true });
    } catch (err) {
      toast.show(err instanceof ApiError ? err.message : t.action_failed, 'error');
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="mx-auto w-full max-w-3xl px-4 py-6 md:py-8">
      <h1 className="mb-4 text-2xl font-bold text-text-primary">{t.new_title}</h1>
      <Card className="space-y-4 p-4 md:p-6">
        <Field label={t.doc_id_label} hint={t.doc_id_hint}>
          <input
            value={docId}
            onChange={(e) => setDocId(e.target.value.toUpperCase())}
            placeholder="QA-PROC-010"
            aria-label={t.doc_id_label}
            className="w-full rounded-md border border-divider bg-bg-paper px-3 py-2 text-sm outline-none focus:border-primary"
          />
        </Field>
        <Field label={t.title_label}>
          <input
            value={title}
            onChange={(e) => setTitle(e.target.value)}
            aria-label={t.title_label}
            className="w-full rounded-md border border-divider bg-bg-paper px-3 py-2 text-sm outline-none focus:border-primary"
          />
        </Field>
        <Field label={t.type_label}>
          <select
            value={type}
            onChange={(e) => setType(e.target.value as DocumentType)}
            aria-label={t.type_label}
            className="w-full rounded-md border border-divider bg-bg-paper px-3 py-2 text-sm outline-none focus:border-primary"
          >
            {DOC_TYPES.map((dt) => (
              <option key={dt} value={dt}>
                {dt}
              </option>
            ))}
          </select>
        </Field>
        <Field label={t.content_label}>
          <textarea
            value={content}
            onChange={(e) => setContent(e.target.value)}
            placeholder={t.content_placeholder}
            aria-label={t.content_label}
            rows={12}
            className="w-full rounded-md border border-divider bg-bg-paper px-3 py-2 font-mono text-sm outline-none focus:border-primary"
          />
        </Field>
        <Button loading={busy} disabled={busy} onClick={() => void onCreate()}>
          {t.create}
        </Button>
      </Card>
    </div>
  );
}

/** Sửa nội dung bản nháp: load → edit → Lưu (PATCH) → Gửi duyệt (submit). */
function EditDraft({ versionId }: { versionId: string }) {
  const navigate = useNavigate();
  const toast = useToast();
  const t = strings.editor;

  const [docId, setDocId] = useState('');
  const [version, setVersion] = useState('');
  const [content, setContent] = useState('');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<ApiError | null>(null);
  const [busy, setBusy] = useState<'save' | 'submit' | null>(null);

  useEffect(() => {
    const ctrl = new AbortController();
    setLoading(true);
    setError(null);
    getVersionContent(versionId, ctrl.signal)
      .then((c) => {
        if (ctrl.signal.aborted) return;
        setDocId(c.doc_id);
        setVersion(c.version);
        setContent(c.content_markdown);
      })
      .catch((err: unknown) => {
        if (ctrl.signal.aborted) return;
        setError(err instanceof ApiError ? err : new ApiError(0, 'NETWORK_ERROR', 'Không kết nối được máy chủ'));
      })
      .finally(() => {
        if (!ctrl.signal.aborted) setLoading(false);
      });
    return () => ctrl.abort();
  }, [versionId]);

  async function onSave() {
    setBusy('save');
    try {
      await updateVersionContent(versionId, content);
      toast.show(t.saved_toast, 'success');
    } catch (err) {
      toast.show(err instanceof ApiError ? err.message : t.action_failed, 'error');
    } finally {
      setBusy(null);
    }
  }

  async function onSubmit() {
    setBusy('submit');
    try {
      await updateVersionContent(versionId, content); // lưu trước khi gửi duyệt
      await submitVersion(versionId);
      toast.show(t.submitted_toast, 'success');
      navigate(`/documents/${docId}`);
    } catch (err) {
      toast.show(err instanceof ApiError ? err.message : t.action_failed, 'error');
    } finally {
      setBusy(null);
    }
  }

  return (
    <div className="mx-auto w-full max-w-3xl px-4 py-6 md:py-8">
      <h1 className="mb-1 text-2xl font-bold text-text-primary">{t.edit_title}</h1>
      {docId && (
        <p className="mb-4 text-sm text-text-secondary">
          {docId} · v{version}
        </p>
      )}

      {loading ? (
        <Skeleton className="h-64 w-full" />
      ) : error ? (
        <Card className="p-4">
          <ErrorState message={error.message} code={error.code} />
        </Card>
      ) : (
        <Card className="space-y-4 p-4 md:p-6">
          <textarea
            value={content}
            onChange={(e) => setContent(e.target.value)}
            aria-label={t.content_label}
            rows={16}
            className="w-full rounded-md border border-divider bg-bg-paper px-3 py-2 font-mono text-sm outline-none focus:border-primary"
          />
          <div className="flex flex-wrap gap-2">
            <Button variant="outlined" loading={busy === 'save'} disabled={busy !== null} onClick={() => void onSave()}>
              {t.save}
            </Button>
            <Button loading={busy === 'submit'} disabled={busy !== null} onClick={() => void onSubmit()}>
              {t.submit}
            </Button>
          </div>
        </Card>
      )}
    </div>
  );
}

function Field({ label, hint, children }: { label: string; hint?: string; children: ReactNode }) {
  return (
    <label className="block">
      <span className="mb-1 block text-sm font-semibold text-text-primary">{label}</span>
      {children}
      {hint && <span className="mt-1 block text-xs text-text-secondary">{hint}</span>}
    </label>
  );
}
