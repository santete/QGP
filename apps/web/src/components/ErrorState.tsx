import { strings } from '../i18n/strings';
import { Button } from './Button';

/**
 * Trạng thái lỗi khi tải dữ liệu. Hiển thị thông điệp + mã lỗi (ISC Error Code)
 * để đối soát log, kèm nút thử lại.
 */
export function ErrorState({
  title,
  message,
  code,
  onRetry,
}: {
  title?: string;
  message?: string;
  code?: string;
  onRetry?: () => void;
}) {
  return (
    <div role="alert" className="flex flex-col items-center justify-center gap-3 py-16 text-center">
      <div className="text-4xl" aria-hidden="true">⚠️</div>
      <h3 className="text-lg font-bold text-text-primary">{title ?? strings.common.error_title}</h3>
      {message && <p className="max-w-md text-sm text-text-secondary">{message}</p>}
      {code && <code className="text-xs text-text-disabled">{code}</code>}
      {onRetry && (
        <Button variant="outlined" size="sm" onClick={onRetry}>
          {strings.common.retry}
        </Button>
      )}
    </div>
  );
}
