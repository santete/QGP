import type { ReactNode } from 'react';
import { Link } from 'react-router-dom';
import { Card } from './Card';
import { Badge, type BadgeColor } from './Badge';
import { StatusBadge } from './StatusBadge';
import type { VersionStatus } from '../api/types';

/** Màu reasonChip theo lý do REC explainable (BR-11) — nguyên văn tiếng Việt do BE trả. */
const REASON_COLOR: Record<string, BadgeColor> = {
  'Bắt buộc': 'error',
  'Cần đọc lại': 'warning',
  'Vừa cập nhật': 'info',
  'Khớp vai trò': 'default',
};

export interface DocumentCardProps {
  docId: string;
  title: string;
  version?: string | null;
  /** reasonChip cho REC (§8): bắt buộc / đọc lại / khớp role / mới. */
  reason?: string | null;
  effectiveDate?: string | null;
  status?: VersionStatus;
  /** Link đích (mặc định /documents/:docId). */
  to?: string;
  /** Meta phụ (badge, ngày…) render dưới tiêu đề. */
  children?: ReactNode;
}

/**
 * Card tài liệu dùng chung (design-system §8) — kết quả tìm / item gợi ý / item lộ trình.
 * Có reasonChip cho REC. Link tới bản Effective.
 */
export function DocumentCard({ docId, title, version, reason, effectiveDate, status, to, children }: DocumentCardProps) {
  return (
    <Link to={to ?? `/documents/${docId}`} className="block">
      <Card className="p-3 transition-shadow hover:shadow-dropdown">
        <div className="flex flex-wrap items-center gap-2">
          <span className="text-xs font-bold uppercase tracking-wide text-text-secondary">{docId}</span>
          {status && <StatusBadge status={status} />}
          {version && <Badge>v{version}</Badge>}
          {reason && <Badge color={REASON_COLOR[reason] ?? 'default'}>{reason}</Badge>}
        </div>
        <h3 className="mt-1 font-semibold text-text-primary">{title}</h3>
        {effectiveDate && <p className="mt-0.5 text-xs text-text-secondary">Áp dụng từ {effectiveDate}</p>}
        {children}
      </Card>
    </Link>
  );
}
