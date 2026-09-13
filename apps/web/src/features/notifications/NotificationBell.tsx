import { useEffect, useRef, useState } from 'react';
import { Link } from 'react-router-dom';
import { useNotifications } from './useNotifications';
import { strings } from '../../i18n/strings';

/** Chuông thông báo (S19, ADM-F-04) — badge số chưa đọc + dropdown danh sách, mark read/all. */
export function NotificationBell() {
  const t = strings.notifications;
  const { items, unreadCount, loading, markRead, markAll } = useNotifications();
  const [open, setOpen] = useState(false);
  const ref = useRef<HTMLDivElement>(null);

  // Đóng khi click ra ngoài.
  useEffect(() => {
    if (!open) return;
    function onClick(e: MouseEvent) {
      if (ref.current && !ref.current.contains(e.target as Node)) setOpen(false);
    }
    document.addEventListener('mousedown', onClick);
    return () => document.removeEventListener('mousedown', onClick);
  }, [open]);

  return (
    <div className="relative" ref={ref}>
      <button
        type="button"
        onClick={() => setOpen((v) => !v)}
        className="relative grid h-9 w-9 place-items-center rounded-md text-text-secondary hover:bg-grey-300/30 hover:text-primary"
        aria-label={t.aria}
        aria-haspopup="true"
        aria-expanded={open}
      >
        <span aria-hidden="true" className="text-lg">🔔</span>
        {unreadCount > 0 && (
          <span
            className="absolute -right-0.5 -top-0.5 grid min-w-4 place-items-center rounded-full bg-error px-1 text-[10px] font-bold leading-4 text-white"
            aria-label={`${unreadCount} ${t.unread}`}
          >
            {unreadCount > 9 ? '9+' : unreadCount}
          </span>
        )}
      </button>

      {open && (
        <div className="absolute right-0 z-50 mt-2 w-80 max-w-[calc(100vw-2rem)] overflow-hidden rounded-lg border border-divider bg-bg-paper shadow-dropdown">
          <div className="flex items-center justify-between border-b border-divider px-3 py-2">
            <span className="text-sm font-bold text-text-primary">{t.title}</span>
            {unreadCount > 0 && (
              <button type="button" onClick={() => void markAll()} className="text-xs font-semibold text-primary hover:underline">
                {t.mark_all}
              </button>
            )}
          </div>

          <ul className="max-h-96 divide-y divide-divider overflow-y-auto">
            {loading ? (
              <li className="px-3 py-4 text-center text-sm text-text-secondary">{strings.common.loading}</li>
            ) : items.length === 0 ? (
              <li className="px-3 py-6 text-center text-sm text-text-secondary">{t.empty}</li>
            ) : (
              items.map((n) => {
                const body = (
                  <div className={`px-3 py-2.5 ${n.read ? '' : 'bg-primary-lighter/30'}`}>
                    <div className="flex items-start gap-2">
                      {!n.read && <span className="mt-1.5 h-2 w-2 shrink-0 rounded-full bg-primary" aria-hidden="true" />}
                      <p className="text-sm text-text-primary">{n.title}</p>
                    </div>
                    <div className="mt-1 flex items-center justify-between">
                      <span className="text-xs text-text-secondary">{n.created_at.slice(0, 10)}</span>
                      {!n.read && (
                        <button
                          type="button"
                          onClick={(e) => {
                            e.preventDefault();
                            void markRead(n.id);
                          }}
                          className="text-xs font-semibold text-primary hover:underline"
                        >
                          {t.mark_read}
                        </button>
                      )}
                    </div>
                  </div>
                );
                return (
                  <li key={n.id}>
                    {n.doc_id ? (
                      <Link to={`/documents/${n.doc_id}`} onClick={() => setOpen(false)} className="block hover:bg-grey-300/20">
                        {body}
                      </Link>
                    ) : (
                      body
                    )}
                  </li>
                );
              })
            )}
          </ul>
        </div>
      )}
    </div>
  );
}
