import { Link } from 'react-router-dom';
import { Card } from '../../components/Card';
import { Badge } from '../../components/Badge';
import type { Onboarding, OnboardingItem } from '../../api/types';
import { strings } from '../../i18n/strings';

interface LearningPathChecklistProps {
  items: OnboardingItem[];
  progress: NonNullable<Onboarding['progress']>;
}

/** Checklist lộ trình học + tiến độ % (§8 LearningPathChecklist, ONB-F-02). Tick khi acked bản Effective. */
export function LearningPathChecklist({ items, progress }: LearningPathChecklistProps) {
  const t = strings.onboarding;

  return (
    <>
      {/* Thanh tiến độ (ONB-F-02). */}
      <div className="mb-6">
        <div className="mb-1 flex items-center justify-between text-sm">
          <span className="font-semibold text-text-primary">{t.progress(progress.read, progress.total)}</span>
          <span className="text-text-secondary">{progress.percent}%</span>
        </div>
        <div className="h-2 w-full overflow-hidden rounded-full bg-grey-300/50">
          <div
            className="h-full rounded-full bg-primary transition-all"
            style={{ width: `${progress.percent}%` }}
            role="progressbar"
            aria-valuenow={progress.percent}
            aria-valuemin={0}
            aria-valuemax={100}
          />
        </div>
        {progress.percent === 100 && <p className="mt-2 text-sm text-success-dark">{t.complete}</p>}
      </div>

      <ol className="space-y-2">
        {items.map((item) => (
          <li key={item.doc_id}>
            <Link to={`/documents/${item.doc_id}`} className="block">
              <Card className="flex items-center gap-3 p-3 transition-shadow hover:shadow-dropdown">
                <span
                  className={`grid h-7 w-7 shrink-0 place-items-center rounded-full text-sm font-bold ${
                    item.acked ? 'bg-success-lighter text-success-darker' : 'bg-grey-300/50 text-grey-700'
                  }`}
                  aria-hidden="true"
                >
                  {item.acked ? '✓' : item.seq}
                </span>
                <div className="min-w-0 flex-1">
                  <div className="flex flex-wrap items-center gap-2">
                    <span className="text-xs font-bold uppercase tracking-wide text-text-secondary">{item.doc_id}</span>
                    {item.effective_version && <Badge>v{item.effective_version}</Badge>}
                    {item.mandatory && <Badge color="error">{t.mandatory}</Badge>}
                    <Badge color={item.acked ? 'success' : 'default'}>{item.acked ? t.done : t.todo}</Badge>
                  </div>
                  <h3 className="mt-0.5 truncate font-semibold text-text-primary">{item.title}</h3>
                </div>
              </Card>
            </Link>
          </li>
        ))}
      </ol>
    </>
  );
}
