/** Nối class name có điều kiện (bỏ falsy). Tránh thêm dep clsx cho scaffold. */
export function cn(...parts: Array<string | false | null | undefined>): string {
  return parts.filter(Boolean).join(' ');
}
