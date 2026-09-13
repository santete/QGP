import { Card } from '../components/Card';
import { Skeleton } from '../components/Skeleton';
import { EmptyState } from '../components/EmptyState';
import { ErrorState } from '../components/ErrorState';
import { LearningPathChecklist } from '../features/onboarding/LearningPathChecklist';
import { useOnboarding } from '../features/onboarding/useOnboarding';
import { strings } from '../i18n/strings';

/** Onboarding S12 (ONB-F-01/02/04) — lộ trình học theo role + tiến độ đọc + checklist tick khi acked. */
export function OnboardingPage() {
  const t = strings.onboarding;
  const { data, loading, error } = useOnboarding();

  const items = data?.items ?? [];
  const progress = data?.progress ?? { total: 0, read: 0, percent: 0 };

  return (
    <div className="mx-auto w-full max-w-3xl px-4 py-6 md:py-8">
      <h1 className="text-2xl font-bold text-text-primary">{data?.title ?? t.title}</h1>
      <p className="mb-4 mt-1 text-sm text-text-secondary">{t.subtitle}</p>

      {loading ? (
        <div className="space-y-3">
          <Skeleton className="h-16 w-full" />
          <Skeleton className="h-16 w-full" />
        </div>
      ) : error ? (
        <Card className="p-4">
          <ErrorState message={error.message} code={error.code} />
        </Card>
      ) : items.length === 0 ? (
        <EmptyState title={t.empty} icon="🧭" />
      ) : (
        <LearningPathChecklist items={items} progress={progress} />
      )}
    </div>
  );
}
