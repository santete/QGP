import type { VersionStatus } from '../api/types';
import { statusLabel } from '../i18n/strings';
import { Badge, type BadgeColor } from './Badge';

/**
 * Badge trạng thái version — 7 trạng thái (state machine SDD §4.3), soft style.
 * Map màu bám ngữ nghĩa: Effective=success, InReview/Approved=warning/info,
 * Superseded/Retired=default (mờ), Draft=default, Published=primary.
 */
const STATUS_COLOR: Record<VersionStatus, BadgeColor> = {
  Draft: 'default',
  InReview: 'warning',
  Approved: 'info',
  Published: 'primary',
  Effective: 'success',
  Superseded: 'default',
  Retired: 'default',
};

export function StatusBadge({ status }: { status: VersionStatus }) {
  return (
    <Badge color={STATUS_COLOR[status]} title={statusLabel(status)}>
      {statusLabel(status)}
    </Badge>
  );
}
