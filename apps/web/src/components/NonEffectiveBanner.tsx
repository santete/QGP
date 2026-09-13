import { strings } from '../i18n/strings';

/**
 * Cảnh báo khi đang xem bản KHÔNG phải Effective (DOC-F-12, S5).
 * Kèm link tới bản Effective nếu server cấp effective_link.
 */
export function NonEffectiveBanner({ effectiveLink }: { effectiveLink?: string | null }) {
  return (
    <div
      role="alert"
      className="flex items-start gap-3 rounded-lg bg-warning-lighter px-4 py-3 text-warning-darker"
    >
      <span aria-hidden="true" className="mt-0.5 text-lg leading-none">⚠️</span>
      <div className="text-sm">
        <p className="font-semibold">{strings.doc.non_effective_warning}</p>
        {effectiveLink && (
          <a href={effectiveLink} className="mt-1 inline-block font-bold underline">
            {strings.doc.view_effective}
          </a>
        )}
      </div>
    </div>
  );
}
