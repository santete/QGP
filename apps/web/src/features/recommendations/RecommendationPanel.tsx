import { DocumentCard } from '../../components/DocumentCard';
import { EmptyState } from '../../components/EmptyState';
import type { RecommendationItem } from '../../api/types';
import { strings } from '../../i18n/strings';

interface RecommendationPanelProps {
  mustRead: RecommendationItem[];
  suggested: RecommendationItem[];
  /** Kiểu hiển thị: 'panel' (Home S2) hoặc 'numbered' (Bắt đầu từ đâu S11 — lộ trình đánh số). */
  variant?: 'panel' | 'numbered';
}

function ItemList({ items, numbered, offset = 0 }: { items: RecommendationItem[]; numbered: boolean; offset?: number }) {
  const ListTag = numbered ? 'ol' : 'ul';
  return (
    <ListTag className="space-y-2">
      {items.map((item, i) => (
        <li key={item.doc_id} className={numbered ? 'flex items-start gap-3' : undefined}>
          {numbered && (
            <span className="mt-0.5 grid h-6 w-6 shrink-0 place-items-center rounded-full bg-primary-lighter text-xs font-bold text-primary-darker">
              {offset + i + 1}
            </span>
          )}
          <div className={numbered ? 'min-w-0 flex-1' : undefined}>
            <DocumentCard
              docId={item.doc_id}
              title={item.title ?? item.doc_id}
              version={item.version}
              reason={item.reason}
              effectiveDate={item.effective_date}
            />
          </div>
        </li>
      ))}
    </ListTag>
  );
}

/**
 * Panel gợi ý REC (§8 RecommendationPanel) — nhóm rõ must_read vs suggested, kèm reasonChip (BR-11).
 * Dùng chung Home S2 (panel) và Bắt đầu từ đâu S11 (numbered).
 */
export function RecommendationPanel({ mustRead, suggested, variant = 'panel' }: RecommendationPanelProps) {
  const t = strings.home;
  const numbered = variant === 'numbered';
  const isEmpty = mustRead.length === 0 && suggested.length === 0;

  if (isEmpty) return <EmptyState title={t.empty} />;

  return (
    <div className="space-y-8">
      <section aria-labelledby="rec-must-read">
        <h2 id="rec-must-read" className="mb-1 text-sm font-bold uppercase tracking-wide text-error-dark">
          {t.must_read}
        </h2>
        {!numbered && <p className="mb-2 text-xs text-text-secondary">{t.must_read_hint}</p>}
        {mustRead.length === 0 ? (
          <p className="text-sm text-text-secondary">{t.empty_must_read}</p>
        ) : (
          <ItemList items={mustRead} numbered={numbered} />
        )}
      </section>

      {suggested.length > 0 && (
        <section aria-labelledby="rec-suggested">
          <h2 id="rec-suggested" className="mb-2 text-sm font-bold uppercase tracking-wide text-text-secondary">
            {t.suggested}
          </h2>
          <ItemList items={suggested} numbered={numbered} offset={numbered ? mustRead.length : 0} />
        </section>
      )}
    </div>
  );
}
