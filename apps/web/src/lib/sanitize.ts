/**
 * Sanitize content_html (Markdown render bởi server — Markdig) trước khi
 * nhúng bằng dangerouslySetInnerHTML. Chống XSS (SECURITY_RULES / R-CODE-005).
 * Server đã render trusted Markdown, nhưng defense-in-depth: luôn sanitize ở FE.
 */
import DOMPurify from 'dompurify';

const ALLOWED_TAGS = [
  'h1', 'h2', 'h3', 'h4', 'h5', 'h6',
  'p', 'a', 'ul', 'ol', 'li', 'strong', 'em', 'code', 'pre',
  'blockquote', 'table', 'thead', 'tbody', 'tr', 'th', 'td',
  'br', 'hr', 'span', 'div', 'img',
];

const ALLOWED_ATTR = ['href', 'title', 'target', 'rel', 'src', 'alt', 'class'];

export function sanitizeHtml(html: string): string {
  return DOMPurify.sanitize(html, {
    ALLOWED_TAGS,
    ALLOWED_ATTR,
    // Chặn javascript: / data: nguy hiểm trên href/src.
    ALLOW_UNKNOWN_PROTOCOLS: false,
  });
}
