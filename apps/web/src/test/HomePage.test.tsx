import { describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import type { Recommendations } from '../api/types';

const h = vi.hoisted(() => ({
  recs: {
    must_read: [
      { doc_id: 'QA-PROC-005', version: '2.1', title: 'Quy trình QA', effective_date: '2026-06-15', reason: 'Bắt buộc' },
    ],
    suggested: [
      { doc_id: 'QA-GUIDE-002', version: '1.3', title: 'Hướng dẫn Test Case', effective_date: '2026-07-20', reason: 'Vừa cập nhật' },
    ],
  } as Recommendations,
}));

vi.mock('../api/client', () => ({
  ApiError: class ApiError extends Error {
    status = 0;
    code = 'X';
  },
  getRecommendations: vi.fn(async (): Promise<Recommendations> => h.recs),
}));

vi.mock('../auth/AuthContext', () => ({
  useAuth: () => ({ user: { sub: 'an@fpt', roles: ['AUTHOR'] }, isAuthenticated: true }),
}));

import { HomePage } from '../pages/HomePage';

describe('HomePage (S2) — trang chủ + gợi ý REC', () => {
  it('hiển thị greeting, nhóm bắt buộc và gợi ý kèm reason (BR-11)', async () => {
    render(
      <MemoryRouter>
        <HomePage />
      </MemoryRouter>,
    );

    // Greeting theo user.sub.
    expect(await screen.findByText(/an@fpt/)).toBeInTheDocument();
    // Must-read.
    expect(screen.getByText('Quy trình QA')).toBeInTheDocument();
    expect(screen.getByText('Bắt buộc')).toBeInTheDocument();
    // Suggested.
    expect(screen.getByText('Hướng dẫn Test Case')).toBeInTheDocument();
    expect(screen.getByText('Vừa cập nhật')).toBeInTheDocument();
  });
});
