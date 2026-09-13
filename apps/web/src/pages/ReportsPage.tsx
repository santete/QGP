import { Link } from 'react-router-dom';
import { Card } from '../components/Card';
import { Badge } from '../components/Badge';
import { Skeleton } from '../components/Skeleton';
import { EmptyState } from '../components/EmptyState';
import { ErrorState } from '../components/ErrorState';
import { DashboardTile } from '../components/DashboardTile';
import { BarChart } from '../components/BarChart';
import { useReports } from '../features/reports/useReports';
import { strings } from '../i18n/strings';

/** Báo cáo quản trị S15 (RPT-F-01..05) — dashboard aggregate: issuance, sức khoẻ, truy cập, compliance, feedback. */
export function ReportsPage() {
  const t = strings.reports;
  const { issuance, compliance, feedback, access, loading, error } = useReports();

  if (loading) {
    return (
      <div className="mx-auto w-full max-w-4xl px-4 py-6 md:py-8">
        <h1 className="text-2xl font-bold text-text-primary">{t.title}</h1>
        <div className="mt-4 grid grid-cols-2 gap-3 md:grid-cols-4">
          {Array.from({ length: 4 }).map((_, i) => (
            <Skeleton key={i} className="h-20 w-full" />
          ))}
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="mx-auto w-full max-w-4xl px-4 py-6 md:py-8">
        <Card className="p-4">
          <ErrorState message={error.message} code={error.code} />
        </Card>
      </div>
    );
  }

  return (
    <div className="mx-auto w-full max-w-4xl px-4 py-6 md:py-8">
      <h1 className="text-2xl font-bold text-text-primary">{t.title}</h1>
      <p className="mb-6 mt-1 text-sm text-text-secondary">{t.subtitle}</p>

      {/* RPT-F-01/02 — issuance + sức khoẻ. */}
      {issuance && (
        <section className="mb-8">
          <h2 className="mb-3 text-sm font-bold uppercase tracking-wide text-text-secondary">{t.issuance}</h2>
          <div className="grid grid-cols-2 gap-3 md:grid-cols-4">
            <DashboardTile label={t.total_documents} value={issuance.total_documents} />
            <DashboardTile label={t.effective_count} value={issuance.effective_count} />
            <DashboardTile label={t.issued_this_month} value={issuance.issued_this_month} />
            <DashboardTile label={t.with_change_summary} value={`${issuance.with_change_summary_percent}%`} />
            <DashboardTile label={t.overdue_review} value={issuance.overdue_review} tone={issuance.overdue_review > 0 ? 'warning' : 'default'} />
            <DashboardTile label={t.due_soon_review} value={issuance.due_soon_review} />
          </div>

          <div className="mt-4 grid gap-3 md:grid-cols-2">
            <BarChart title={t.by_type} data={Object.entries(issuance.by_type).map(([label, value]) => ({ label, value }))} />
            <BarChart title={t.by_status} data={Object.entries(issuance.by_version_status).map(([label, value]) => ({ label, value }))} />
          </div>
        </section>
      )}

      {/* RPT-F-03 — truy cập (proxy theo ack; aggregate). */}
      {access && (
        <section className="mb-8">
          <h2 className="mb-1 text-sm font-bold uppercase tracking-wide text-text-secondary">{t.access}</h2>
          <p className="mb-3 text-xs italic text-text-secondary">{access.note}</p>
          <div className="grid gap-3 md:grid-cols-2">
            <Card className="p-4">
              <div className="mb-2 text-xs font-bold uppercase tracking-wide text-text-secondary">{t.access_top}</div>
              {access.top_engaged.length === 0 ? (
                <p className="text-sm text-text-secondary">—</p>
              ) : (
                <ul className="space-y-1.5 text-sm">
                  {access.top_engaged.map((d) => (
                    <li key={d.doc_id} className="flex items-center justify-between gap-2">
                      <Link to={`/documents/${d.doc_id}`} className="truncate text-text-primary hover:text-primary">{d.title}</Link>
                      <Badge color="success">{t.access_ack_unit(d.ack_count)}</Badge>
                    </li>
                  ))}
                </ul>
              )}
            </Card>
            <Card className="p-4">
              <div className="mb-2 text-xs font-bold uppercase tracking-wide text-text-secondary">
                {t.access_zero} ({access.effective_with_zero_ack})
              </div>
              {access.zero_ack_docs.length === 0 ? (
                <p className="text-sm text-text-secondary">—</p>
              ) : (
                <ul className="space-y-1.5 text-sm">
                  {access.zero_ack_docs.map((d) => (
                    <li key={d.doc_id} className="flex items-center justify-between gap-2">
                      <Link to={`/documents/${d.doc_id}`} className="truncate text-text-primary hover:text-primary">{d.title}</Link>
                      <Badge color="warning">0</Badge>
                    </li>
                  ))}
                </ul>
              )}
            </Card>
          </div>
        </section>
      )}

      {/* RPT-F-05 — feedback. */}
      {feedback && (
        <section className="mb-8">
          <h2 className="mb-3 text-sm font-bold uppercase tracking-wide text-text-secondary">{t.feedback}</h2>
          <div className="grid gap-3 md:grid-cols-3">
            <DashboardTile label={t.fb_total} value={feedback.total} />
            <DashboardTile label={t.fb_avg_resolution} value={feedback.avg_resolution_hours ?? '—'} />
            <div className="md:col-span-1">
              <BarChart title={t.fb_by_status} data={Object.entries(feedback.by_status).map(([label, value]) => ({ label, value }))} />
            </div>
          </div>
        </section>
      )}

      {/* RPT-F-04 — compliance. */}
      {compliance && (
        <section>
          <div className="mb-3 flex flex-wrap items-baseline justify-between gap-2">
            <h2 className="text-sm font-bold uppercase tracking-wide text-text-secondary">{t.compliance}</h2>
            <span className="text-xs text-text-secondary">{t.compliance_sub(compliance.fully_compliant_docs, compliance.mandatory_docs)}</span>
          </div>
          {compliance.items.length === 0 ? (
            <EmptyState title={t.empty_compliance} icon="📋" />
          ) : (
            <div className="space-y-2">
              {compliance.items.map((item) => (
                <Card key={item.doc_id} className="p-3">
                  <div className="flex flex-wrap items-center justify-between gap-2">
                    <div className="min-w-0">
                      <span className="text-xs font-bold uppercase tracking-wide text-text-secondary">{item.doc_id}</span>
                      <h3 className="truncate font-semibold text-text-primary">{item.title}</h3>
                    </div>
                    <Badge color={item.percent === 100 ? 'success' : item.percent >= 50 ? 'warning' : 'error'}>
                      {item.acked_count}/{item.audience_count} · {item.percent}%
                    </Badge>
                  </div>
                  <div className="mt-2 h-1.5 w-full overflow-hidden rounded-full bg-grey-300/50">
                    <div className="h-full rounded-full bg-primary" style={{ width: `${item.percent}%` }} />
                  </div>
                  {item.not_read.length > 0 && (
                    <p className="mt-2 text-xs text-text-secondary">
                      <span className="font-semibold">{t.not_read_label}:</span> {item.not_read.join(', ')}
                    </p>
                  )}
                </Card>
              ))}
            </div>
          )}
        </section>
      )}
    </div>
  );
}
