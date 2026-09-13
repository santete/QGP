import { describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import type { Onboarding } from '../api/types';

const h = vi.hoisted(() => ({
  data: {
    title: 'Nhập môn QA',
    roles: ['READER'],
    progress: { total: 2, read: 1, percent: 50 },
    items: [
      { doc_id: 'QA-PROC-005', title: 'Quy trình QA', seq: 1, mandatory: true, effective_version: '2.1', acked: true },
      { doc_id: 'QA-POL-001', title: 'Chính sách Chất lượng', seq: 2, mandatory: true, effective_version: '1.0', acked: false },
    ],
  } as Onboarding,
}));

vi.mock('../api/client', () => ({
  ApiError: class ApiError extends Error {
    status = 0;
    code = 'X';
  },
  getOnboarding: vi.fn(async (): Promise<Onboarding> => h.data),
}));

import { OnboardingPage } from '../pages/OnboardingPage';

describe('OnboardingPage (S12) — lộ trình nhập môn', () => {
  it('hiển thị tiến độ % và trạng thái đã đọc/chưa đọc từng item', async () => {
    render(
      <MemoryRouter>
        <OnboardingPage />
      </MemoryRouter>,
    );

    expect(await screen.findByText('Quy trình QA')).toBeInTheDocument();
    expect(screen.getByText('Chính sách Chất lượng')).toBeInTheDocument();
    // Tiến độ 50%.
    expect(screen.getByText('50%')).toBeInTheDocument();
    const bar = screen.getByRole('progressbar');
    expect(bar).toHaveAttribute('aria-valuenow', '50');
    // Trạng thái từng item.
    expect(screen.getByText('Đã đọc')).toBeInTheDocument();
    expect(screen.getByText('Chưa đọc')).toBeInTheDocument();
  });
});
