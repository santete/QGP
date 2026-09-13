import type { ReactNode } from 'react';
import { cn } from '../lib/cn';

export type BadgeColor = 'default' | 'primary' | 'info' | 'success' | 'warning' | 'error';

interface BadgeProps {
  color?: BadgeColor;
  children: ReactNode;
  className?: string;
  title?: string;
}

// Soft badge (bg lighter + fg darker) — Minimal UI style.
const colors: Record<BadgeColor, string> = {
  default: 'bg-grey-300/50 text-grey-700',
  primary: 'bg-primary-lighter text-primary-darker',
  info: 'bg-info-lighter text-info-darker',
  success: 'bg-success-lighter text-success-darker',
  warning: 'bg-warning-lighter text-warning-darker',
  error: 'bg-error-lighter text-error-darker',
};

export function Badge({ color = 'default', className, children, title }: BadgeProps) {
  return (
    <span
      className={cn(
        'inline-flex items-center gap-1 rounded-md px-2 py-0.5 text-xs font-bold leading-5',
        colors[color],
        className,
      )}
      title={title}
    >
      {children}
    </span>
  );
}
