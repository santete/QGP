import type { HTMLAttributes, ReactNode } from 'react';
import { cn } from '../lib/cn';

interface CardProps extends HTMLAttributes<HTMLDivElement> {
  children: ReactNode;
}

// radius xl (16px), shadow-card (not border) — Minimal UI.
export function Card({ className, children, ...rest }: CardProps) {
  return (
    <div
      className={cn('rounded-xl bg-bg-paper shadow-card', className)}
      {...rest}
    >
      {children}
    </div>
  );
}
