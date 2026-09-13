import { useCallback, useEffect, useState } from 'react';
import { ApiError, getNotifications, markAllNotificationsRead, markNotificationRead } from '../../api/client';
import type { Notification } from '../../api/types';

interface NotificationsState {
  items: Notification[];
  unreadCount: number;
  loading: boolean;
  error: ApiError | null;
  markRead: (id: string) => Promise<void>;
  markAll: () => Promise<void>;
  reload: () => void;
}

/** Hộp thư thông báo (S19, ADM-F-04) — list + số chưa đọc + mark read/all. */
export function useNotifications(): NotificationsState {
  const [items, setItems] = useState<Notification[]>([]);
  const [unreadCount, setUnreadCount] = useState(0);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<ApiError | null>(null);
  const [nonce, setNonce] = useState(0);

  const reload = useCallback(() => setNonce((n) => n + 1), []);

  useEffect(() => {
    const ctrl = new AbortController();
    setLoading(true);
    setError(null);
    getNotifications(false, ctrl.signal)
      .then((list) => {
        if (ctrl.signal.aborted) return;
        setItems(list.data);
        setUnreadCount(list.unread_count);
      })
      .catch((err: unknown) => {
        if (ctrl.signal.aborted) return;
        setError(err instanceof ApiError ? err : new ApiError(0, 'NETWORK_ERROR', 'Không kết nối được máy chủ'));
      })
      .finally(() => {
        if (!ctrl.signal.aborted) setLoading(false);
      });
    return () => ctrl.abort();
  }, [nonce]);

  const markRead = useCallback(async (id: string) => {
    await markNotificationRead(id);
    setItems((prev) => prev.map((n) => (n.id === id ? { ...n, read: true } : n)));
    setUnreadCount((c) => Math.max(0, c - 1));
  }, []);

  const markAll = useCallback(async () => {
    await markAllNotificationsRead();
    setItems((prev) => prev.map((n) => ({ ...n, read: true })));
    setUnreadCount(0);
  }, []);

  return { items, unreadCount, loading, error, markRead, markAll, reload };
}
