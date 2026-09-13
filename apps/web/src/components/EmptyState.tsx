import type { ReactNode } from 'react';

export function EmptyState({
  title,
  description,
  icon = '📄',
  action,
}: {
  title: string;
  description?: string;
  icon?: ReactNode;
  action?: ReactNode;
}) {
  return (
    <div className="flex flex-col items-center justify-center gap-2 py-16 text-center">
      <div className="text-4xl" aria-hidden="true">{icon}</div>
      <h3 className="text-lg font-bold text-text-primary">{title}</h3>
      {description && <p className="max-w-sm text-sm text-text-secondary">{description}</p>}
      {action}
    </div>
  );
}
