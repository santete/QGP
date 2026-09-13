import { describe, expect, it, vi, beforeEach } from 'vitest';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { ToastProvider } from '../components/Toast';
import type { ReviewQueueItem } from '../api/types';

const h = vi.hoisted(() => ({
  approved: [
    {
      version_id: 'a-1',
      doc_id: 'QA-POL-003',
      title: 'Chính sách Bảo mật',
      version: '1.0',
      submitted_at: '2026-07-18T09:00:00Z',
    },
  ] as ReviewQueueItem[],
  publishVersion: vi.fn(async () => ({})),
}));

vi.mock('../api/client', () => ({
  ApiError: class ApiError extends Error {
    status = 0;
    code = 'X';
  },
  listReviewQueue: vi.fn(async (): Promise<ReviewQueueItem[]> => h.approved),
  publishVersion: h.publishVersion,
}));

import { PublishPage } from '../pages/PublishPage';

function renderPage() {
  return render(
    <MemoryRouter>
      <ToastProvider>
        <PublishPage />
      </ToastProvider>
    </MemoryRouter>,
  );
}

describe('PublishPage (S8) — chờ ban hành', () => {
  beforeEach(() => h.publishVersion.mockClear());

  it('mở form + ban hành gọi publishVersion và gỡ item', async () => {
    renderPage();
    expect(await screen.findByText('Chính sách Bảo mật')).toBeInTheDocument();

    // Mở form (nút "Ban hành" đầu tiên).
    fireEvent.click(screen.getByRole('button', { name: 'Ban hành' }));
    const summary = await screen.findByLabelText('Tóm tắt thay đổi');
    fireEvent.change(summary, { target: { value: 'Ban hành lần đầu' } });

    fireEvent.click(screen.getByRole('button', { name: 'Xác nhận ban hành' }));

    await waitFor(() =>
      expect(h.publishVersion).toHaveBeenCalledWith(
        'a-1',
        expect.objectContaining({ change_summary: 'Ban hành lần đầu' }),
      ),
    );
    await waitFor(() => expect(screen.queryByText('Chính sách Bảo mật')).not.toBeInTheDocument());
  });

  it('BR-07: ngày áp dụng < ngày ban hành → KHÔNG gọi publish', async () => {
    renderPage();
    await screen.findByText('Chính sách Bảo mật');
    fireEvent.click(screen.getByRole('button', { name: 'Ban hành' }));

    const summary = await screen.findByLabelText('Tóm tắt thay đổi');
    fireEvent.change(summary, { target: { value: 'x' } });
    fireEvent.change(screen.getByLabelText('Ngày ban hành'), { target: { value: '2026-07-20' } });
    fireEvent.change(screen.getByLabelText('Ngày áp dụng'), { target: { value: '2026-07-01' } });

    fireEvent.click(screen.getByRole('button', { name: 'Xác nhận ban hành' }));
    expect(h.publishVersion).not.toHaveBeenCalled();
  });
});
