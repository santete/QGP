import { Card } from './Card';

export interface BarDatum {
  label: string;
  value: number;
}

interface BarChartProps {
  title: string;
  data: BarDatum[];
  /** Token màu semantic (mặc định primary). */
  colorVar?: string;
  emptyLabel?: string;
}

/**
 * Biểu đồ cột ngang aggregate (§8 Chart) — CSS thuần, on-token (Minimal UI).
 * Accessible: mỗi hàng có nhãn + số; không chỉ dựa vào màu (C3/WCAG).
 */
export function BarChart({ title, data, colorVar = 'var(--color-primary-main)', emptyLabel = '—' }: BarChartProps) {
  const max = Math.max(1, ...data.map((d) => d.value));
  return (
    <Card className="p-4">
      <div className="mb-2 text-xs font-bold uppercase tracking-wide text-text-secondary">{title}</div>
      {data.length === 0 ? (
        <p className="text-sm text-text-secondary">{emptyLabel}</p>
      ) : (
        <ul className="space-y-1.5">
          {data.map((d) => (
            <li key={d.label} className="text-sm">
              <div className="mb-0.5 flex items-center justify-between">
                <span className="text-text-primary">{d.label}</span>
                <span className="font-semibold text-text-secondary">{d.value}</span>
              </div>
              <div className="h-2 w-full overflow-hidden rounded-full bg-grey-300/40">
                <div
                  className="h-full rounded-full"
                  style={{ width: `${Math.round((d.value / max) * 100)}%`, backgroundColor: colorVar }}
                  role="img"
                  aria-label={`${d.label}: ${d.value}`}
                />
              </div>
            </li>
          ))}
        </ul>
      )}
    </Card>
  );
}
