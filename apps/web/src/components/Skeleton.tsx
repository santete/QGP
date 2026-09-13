import { cn } from '../lib/cn';

export function Skeleton({ className }: { className?: string }) {
  return (
    <div
      className={cn('animate-pulse rounded-md bg-grey-300/60', className)}
      aria-hidden="true"
    />
  );
}

/** Skeleton cho màn Document view (S4) khi đang tải. */
export function DocumentSkeleton() {
  return (
    <div className="space-y-4" data-testid="document-skeleton">
      <Skeleton className="h-8 w-2/3" />
      <div className="flex gap-2">
        <Skeleton className="h-6 w-24" />
        <Skeleton className="h-6 w-32" />
      </div>
      <Skeleton className="h-4 w-full" />
      <Skeleton className="h-4 w-11/12" />
      <Skeleton className="h-4 w-10/12" />
      <Skeleton className="h-40 w-full" />
    </div>
  );
}
