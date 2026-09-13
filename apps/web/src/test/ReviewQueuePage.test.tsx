import { describe, expect, it, vi, beforeEach } from 'vitest';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { ToastProvider } from '../components/Toast';
import type { ReviewQueueItem } from '../api/types';

const fx = vi.hoisted(() => ({
  rows: [
    {
      version_id: 'v-111',
      doc_id: 'QA-PROC-007',
      title: 'Quy trình Quản lý Rủi ro',
      version: '1.3',
      submitted_at: '2026-07-20T02:15:00Z',
    },
    {
      version_id: 'v-222',
      doc_id: 'QA-WI-012',
      title: 'Hướng dẫn Kiểm thử Hồi quy',
      version: '2.0',
      submitted_at: '2026-07-22T07:40:00Z',
    },
  ] as ReviewQueueItem[],
  approveVersion: vi.fn(async () => ({})),
}));

const approveVersion = fx.approveVersion;

vi.mock('../api/client', () => ({
  ApiError: class ApiError extends Error {
    status = 0;
    code = 'X';
  },
  listReviewQueue: vi.fn(async (): Promise<ReviewQueueItem[]> => fx.rows),
  approveVersion: fx.approveVersion,
}));

import { ReviewQueuePage } from '../pages/ReviewQueuePage';

function renderPage() {
  return render(
    <MemoryRouter>
      <ToastProvider>
        <ReviewQueuePage />
      </ToastProvider>
    </MemoryRouter>,
  );
}

describe('ReviewQueuePage (S9)', () => {
  beforeEach(() => approveVersion.mockClear());

  it('render hàng đợi duyệt với doc_id, version, link chi tiết', async () => {
    renderPage();
    expect(await screen.findByText('Quy trình Quản lý Rủi ro')).toBeInTheDocument();
    expect(screen.getByText('QA-PROC-007')).toBeInTheDocument();
    expect(screen.getByText('v1.3')).toBeInTheDocument();
    const link = screen.getAllByRole('link', { name: /Xem tài liệu/i })[0];
    expect(link).toHaveAttribute('href', '/documents/QA-PROC-007');
  });

  it('approve gọi API decision=approve và gỡ item khỏi hàng đợi', async () => {
    renderPage();
    await screen.findByText('Quy trình Quản lý Rủi ro');

    fireEvent.click(screen.getAllByRole('button', { name: 'Duyệt' })[0]);

    expect(approveVersion).toHaveBeenCalledWith('v-111', 'approve', undefined);
    await waitFor(() => expect(screen.queryByText('Quy trình Quản lý Rủi ro')).not.toBeInTheDocument());
  });

  it('reject cần comment: mở ô lý do, nhập rồi xác nhận → decision=reject kèm comment', async () => {
    renderPage();
    await screen.findByText('Quy trình Quản lý Rủi ro');

    // Lần 1: mở ô nhập lý do (chưa gọi API).
    fireEvent.click(screen.getAllByRole('button', { name: 'Từ chối' })[0]);
    expect(approveVersion).not.toHaveBeenCalled();

    const box = await screen.findByPlaceholderText(/Lý do từ chối/i);
    fireEvent.change(box, { target: { value: 'Thiếu tiêu chí chấp nhận' } });

    // Lần 2: xác nhận từ chối.
    fireEvent.click(screen.getAllByRole('button', { name: 'Từ chối' })[0]);
    expect(approveVersion).toHaveBeenCalledWith('v-111', 'reject', 'Thiếu tiêu chí chấp nhận');
  });
});
