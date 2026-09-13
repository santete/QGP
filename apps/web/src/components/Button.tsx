import type { ButtonHTMLAttributes, ReactNode } from 'react';
import { cn } from '../lib/cn';

type Variant = 'contained' | 'soft' | 'outlined' | 'text';
type Size = 'sm' | 'md' | 'lg';

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: Variant;
  size?: Size;
  loading?: boolean;
  children: ReactNode;
}

// radius md (8px), shadow-not-border, motion 180ms — Minimal UI.
const base =
  'inline-flex items-center justify-center gap-2 rounded-md font-bold whitespace-nowrap ' +
  'transition-all duration-[180ms] ease-[cubic-bezier(0.4,0,0.2,1)] ' +
  'focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary/40 ' +
  'disabled:opacity-50 disabled:pointer-events-none';

const variants: Record<Variant, string> = {
  contained: 'bg-primary text-white shadow-primary hover:bg-primary-dark',
  soft: 'bg-primary-lighter text-primary-darker hover:bg-primary-light/60',
  outlined: 'border border-primary/50 text-primary hover:bg-primary-lighter/40',
  text: 'text-primary hover:bg-primary-lighter/40',
};

const sizes: Record<Size, string> = {
  sm: 'h-8 px-3 text-[0.8125rem]',
  md: 'h-10 px-4 text-sm',
  lg: 'h-12 px-5 text-base',
};

export function Button({
  variant = 'contained',
  size = 'md',
  loading = false,
  disabled,
  className,
  children,
  ...rest
}: ButtonProps) {
  return (
    <button
      className={cn(base, variants[variant], sizes[size], className)}
      disabled={disabled || loading}
      aria-busy={loading || undefined}
      {...rest}
    >
      {loading && (
        <span
          className="h-4 w-4 animate-spin rounded-full border-2 border-current border-t-transparent"
          aria-hidden="true"
        />
      )}
      {children}
    </button>
  );
}
