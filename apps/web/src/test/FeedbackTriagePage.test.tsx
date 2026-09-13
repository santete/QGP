import { describe, expect, it, vi, beforeEach } from 'vitest';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { ToastProvider } from '../components/Toast';
import type { Feedback } from '../api/types';

const h = vi.hoisted(() => ({
  rows: [
    {
      id: 'fb-1',
      version_id: 'v-21',
      doc_id: 'QA-PROC-005',
      version: '2.1',
      category: 'content_error',
      status: 'New',
      body: 'Bước 5 sai thứ tự.',
      created_at: '2026-07-21T04:10:00Z',
    },
  ] as Feedback[],
  triageFeedback: vi.fn(async (_id: string, status: string) => ({ ...h.rows[0], status })),
}));

vi.mock('../api/client', () => ({
  ApiError: class ApiError extends Error {
    status = 0;
    code = 'X';
  },
  listFeedback: vi.fn(async (): Promise<Feedback[]> => h.rows),
  triageFeedback: h.triageFeedback,
}));

import { FeedbackTriagePage } from '../pages/FeedbackTriagePage';

function renderPage() {
  return render(
    <MemoryRouter>
      <ToastProvider>
        <FeedbackTriagePage />
      </ToastProvider>
    </MemoryRouter>,
  );
}

describe('FeedbackTriagePage (S14)', () => {
  beforeEach(() => h.triageFeedback.mockClear());

  it('render feedback + đổi trạng thái gọi triageFeedback', async () => {
    renderPage();
    expect(await screen.findByText('Bước 5 sai thứ tự.')).toBeInTheDocument();
    expect(screen.getByText('QA-PROC-005')).toBeInTheDocument();

    fireEvent.change(screen.getByLabelText('status-fb-1'), { target: { value: 'Triaged' } });

    await waitFor(() => expect(h.triageFeedback).toHaveBeenCalledWith('fb-1', 'Triaged'));
  });
});
