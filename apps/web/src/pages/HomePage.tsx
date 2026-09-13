import { Link } from 'react-router-dom';
import { Card } from '../components/Card';
import { Skeleton } from '../components/Skeleton';
import { ErrorState } from '../components/ErrorState';
import { RecommendationPanel } from '../features/recommendations/RecommendationPanel';
import { useRecommendations } from '../features/recommendations/useRecommendations';
import { useAuth } from '../auth/AuthContext';
import { strings } from '../i18n/strings';

/** Trang chủ (S2, REC-F-02) — greeting + gợi ý explainable theo role (BR-11): bắt buộc đọc + gợi ý. */
export function HomePage() {
  const t = strings.home;
  const { user } = useAuth();
  const { data, loading, error } = useRecommendations();

  const mustRead = data?.must_read ?? [];
  const suggested = data?.suggested ?? [];

  return (
    <div className="mx-auto w-full max-w-3xl px-4 py-6 md:py-8">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h1 className="text-2xl font-bold text-text-primary">{t.greeting(user?.sub ?? '')}</h1>
          <p className="mb-4 mt-1 text-sm text-text-secondary">{t.subtitle}</p>
        </div>
        <Link
          to="/start"
          className="rounded-md border border-divider bg-bg-paper px-3 py-2 text-sm font-semibold text-primary hover:border-primary"
        >
          {t.start_cta}
        </Link>
      </div>

      {loading ? (
        <div className="space-y-3">
          <Skeleton className="h-20 w-full" />
          <Skeleton className="h-20 w-full" />
        </div>
      ) : error ? (
        <Card className="p-4">
          <ErrorState message={error.message} code={error.code} />
        </Card>
      ) : (
        <RecommendationPanel mustRead={mustRead} suggested={suggested} variant="panel" />
      )}
    </div>
  );
}
