import { useMemo } from 'react';
import { Card } from '../../components/Card';
import { StatusBadge } from '../../components/StatusBadge';
import { EffectiveDateBadge } from '../../components/EffectiveDateBadge';
import { NonEffectiveBanner } from '../../components/NonEffectiveBanner';
import { DocumentSkeleton } from '../../components/Skeleton';
import { ErrorState } from '../../components/ErrorState';
import { Badge } from '../../components/Badge';
import { sanitizeHtml } from '../../lib/sanitize';
import { strings } from '../../i18n/strings';
import { AckButton } from './AckButton';
import { useDocument } from './useDocument';

/**
 * Màn Chi tiết tài liệu (S4/S5) — vertical slice Sprint 1.
 * GET /documents/{doc_id} → render Markdown (sanitize) + StatusBadge +
 * EffectiveDateBadge + NonEffectiveBanner + acknowledge.
 */
export function DocumentView({ docId }: { docId: string }) {
  const { data, versions, effectiveVersionId, loading, error, reload } = useDocument(docId);

  const effectiveDate = useMemo(
    () => versions.find((v) => v.id === effectiveVersionId)?.effective_date ?? null,
    [versions, effectiveVersionId],
  );

  if (loading) {
    return (
      <Card className="p-6 md:p-8">
        <DocumentSkeleton />
      </Card>
    );
  }

  if (error || !data) {
    const notFound = error?.status === 404;
    return (
      <Card className="p-6 md:p-8">
        <ErrorState
          title={notFound ? strings.common.not_found_title : strings.common.error_title}
          message={error?.message}
          code={error?.code}
          onRetry={notFound ? undefined : reload}
        />
      </Card>
    );
  }

  const safeHtml = data.content_html ? sanitizeHtml(data.content_html) : '';

  return (
    <article className="space-y-4">
      {data.is_effective === false && <NonEffectiveBanner effectiveLink={data.effective_link} />}

      <Card className="p-6 md:p-8">
        <header className="space-y-3 border-b border-dashed border-divider pb-5">
          <div className="flex flex-wrap items-center gap-2">
            <span className="text-xs font-bold uppercase tracking-wide text-text-secondary">
              {data.doc_id}
            </span>
            <StatusBadge status={data.status} />
            <EffectiveDateBadge effectiveDate={effectiveDate} badge={data.badge} />
          </div>

          <h1 className="text-2xl font-bold leading-tight text-text-primary md:text-3xl">
            {data.title}
          </h1>

          <dl className="flex flex-wrap gap-x-6 gap-y-1 text-sm text-text-secondary">
            {data.effective_version && (
              <div className="flex gap-1">
                <dt className="font-medium">{strings.doc.effective_version}:</dt>
                <dd>{data.effective_version}</dd>
              </div>
            )}
            {data.next_review_date && (
              <div className="flex gap-1">
                <dt className="font-medium">{strings.doc.review_due}:</dt>
                <dd>{data.next_review_date}</dd>
              </div>
            )}
          </dl>

          {data.tags && data.tags.length > 0 && (
            <div className="flex flex-wrap items-center gap-1.5">
              {data.tags.map((tag) => (
                <Badge key={tag}>#{tag}</Badge>
              ))}
            </div>
          )}
        </header>

        {data.mandatory_ack && effectiveVersionId && (
          <div className="pt-5">
            <AckButton versionId={effectiveVersionId} />
          </div>
        )}

        <div
          className="doc-prose pt-6"
          data-testid="doc-content"
          // content_html đã sanitize (DOMPurify) — chống XSS.
          dangerouslySetInnerHTML={{ __html: safeHtml }}
        />
      </Card>
    </article>
  );
}
