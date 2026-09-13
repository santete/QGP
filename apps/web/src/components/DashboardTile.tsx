import type { ReactNode } from 'react';
import { Card } from './Card';

interface DashboardTileProps {
  label: string;
  value: number | string;
  /** Nhấn cảnh báo (vd quá hạn review > 0). */
  tone?: 'default' | 'warning' | 'error' | 'success';
  hint?: ReactNode;
}

const TONE: Record<NonNullable<DashboardTileProps['tone']>, string> = {
  default: 'text-text-primary',
  warning: 'text-warning-dark',
  error: 'text-error-dark',
  success: 'text-success-dark',
};

/** KPI tile cho dashboard (§8 DashboardTile) — 1 con số + nhãn. Tách khỏi dữ liệu thô (C8). */
export function DashboardTile({ label, value, tone = 'default', hint }: DashboardTileProps) {
  return (
    <Card className="p-4">
      <div className={`text-2xl font-bold ${TONE[tone]}`}>{value}</div>
      <div className="mt-0.5 text-xs text-text-secondary">{label}</div>
      {hint && <div className="mt-1 text-xs text-text-secondary">{hint}</div>}
    </Card>
  );
}
