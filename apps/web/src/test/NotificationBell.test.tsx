import { describe, expect, it, vi } from 'vitest';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import type { Notification, NotificationList } from '../api/types';

const h = vi.hoisted(() => ({
  list: {
    data: [
      { id: 'n1', type: 'doc_effective', doc_id: 'QA-PROC-005', title: 'QA-PROC-005 đã ban hành 2.1', read: false, created_at: '2026-07-25T09:00:00Z' },
      { id: 'n2', type: 'doc_effective', doc_id: 'QA-POL-001', title: 'QA-POL-001 đã ban hành 1.0', read: true, created_at: '2026-07-20T09:00:00Z' },
    ],
    unread_count: 1,
  } as NotificationList,
}));

vi.mock('../api/client', () => ({
  ApiError: class ApiError extends Error {
    status = 0;
    code = 'X';
  },
  getNotifications: vi.fn(async (): Promise<NotificationList> => h.list),
  markNotificationRead: vi.fn(async (id: string): Promise<Notification> => ({ ...h.list.data[0], id, read: true })),
  markAllNotificationsRead: vi.fn(async (): Promise<{ updated: number }> => ({ updated: 1 })),
}));

import { NotificationBell } from '../features/notifications/NotificationBell';

describe('NotificationBell (S19) — chuông thông báo', () => {
  it('hiện badge số chưa đọc và mở dropdown liệt kê thông báo', async () => {
    render(
      <MemoryRouter>
        <NotificationBell />
      </MemoryRouter>,
    );

    // Badge số chưa đọc = 1.
    expect(await screen.findByLabelText(/1 Chưa đọc/)).toBeInTheDocument();

    // Mở dropdown → thấy thông báo.
    fireEvent.click(screen.getByLabelText('Thông báo'));
    await waitFor(() => expect(screen.getByText('QA-PROC-005 đã ban hành 2.1')).toBeInTheDocument());
    expect(screen.getByText('QA-POL-001 đã ban hành 1.0')).toBeInTheDocument();
  });
});
