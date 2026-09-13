import { Badge } from './Badge';

/** Hiển thị ngày áp dụng. Nếu server cấp `badge` (vd "Sắp áp dụng…") thì ưu tiên. */
export function EffectiveDateBadge({
  effectiveDate,
  badge,
}: {
  effectiveDate?: string | null;
  badge?: string | null;
}) {
  if (badge) return <Badge color="warning">{badge}</Badge>;
  if (!effectiveDate) return null;
  const label = formatDate(effectiveDate);
  return <Badge color="success">Áp dụng từ {label}</Badge>;
}

function formatDate(iso: string): string {
  // iso dạng yyyy-mm-dd → dd/mm/yyyy (không phụ thuộc locale runtime).
  const m = /^(\d{4})-(\d{2})-(\d{2})/.exec(iso);
  return m ? `${m[3]}/${m[2]}/${m[1]}` : iso;
}
