import { Link, useParams } from 'react-router-dom';
import { DocumentView } from '../features/documents/DocumentView';
import { FeedbackButton } from '../features/feedback/FeedbackButton';
import { SubscribeButton } from '../features/subscriptions/SubscribeButton';
import { EmptyState } from '../components/EmptyState';
import { strings } from '../i18n/strings';

export function DocumentPage() {
  const { docId } = useParams<{ docId: string }>();

  if (!docId) {
    return <EmptyState title="Thiếu mã tài liệu" description="URL không hợp lệ." />;
  }

  return (
    <div className="mx-auto w-full max-w-3xl px-4 py-6 md:py-8">
      <div className="mb-3 flex items-center justify-end gap-3">
        <Link
          to={`/documents/${docId}/history`}
          className="text-sm font-semibold text-primary hover:underline"
        >
          🕓 {strings.history.open}
        </Link>
        <SubscribeButton docId={docId} />
        <FeedbackButton docId={docId} />
      </div>
      <DocumentView docId={docId} />
    </div>
  );
}
