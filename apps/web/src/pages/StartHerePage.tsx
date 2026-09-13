import { Link } from 'react-router-dom';
import { Card } from '../components/Card';
import { Skeleton } from '../components/Skeleton';
import { EmptyState } from '../components/EmptyState';
import { ErrorState } from '../components/ErrorState';
import { RecommendationPanel } from '../features/recommendations/RecommendationPanel';
import { useRecommendations } from '../features/recommendations/useRecommendations';
import { strings } from '../i18n/strings';

/**
 * "Bắt đầu từ đâu" (S11, REC-F-02) — lộ trình đọc ưu tiên theo role (BR-11):
 * bắt buộc trước, gợi ý sau. Dùng chung RecommendationPanel (variant numbered).
 */
export function StartHerePage() {
  const t = strings.start;
  const { data, loading, error } = useRecommendations();

  const mustRead = data?.must_read ?? [];
  const suggested = data?.suggested ?? [];
  const isEmpty = mustRead.length === 0 && suggested.length === 0;

  return (
    <div className="mx-auto w-full max-w-3xl px-4 py-6 md:py-8">
      <h1 className="text-2xl font-bold text-text-primary">{t.title}</h1>
      <p className="mb-4 mt-1 text-sm text-text-secondary">{t.subtitle}</p>

      {loading ? (
        <div className="space-y-3">
          <Skeleton className="h-20 w-full" />
          <Skeleton className="h-20 w-full" />
        </div>
      ) : error ? (
        <Card className="p-4">
          <ErrorState message={error.message} code={error.code} />
        </Card>
      ) : isEmpty ? (
        <EmptyState
          title={t.empty}
          action={
            <Link to="/kb" className="text-sm font-semibold text-primary hover:underline">
              {t.to_kb}
            </Link>
          }
        />
      ) : (
        <RecommendationPanel mustRead={mustRead} suggested={suggested} variant="numbered" />
      )}
    </div>
  );
}
