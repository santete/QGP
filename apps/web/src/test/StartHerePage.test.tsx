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
      { doc_id: 'QA-GUIDE-002', version: '1.3', title: 'Hướng dẫn Test Case', effective_date: '2026-07-20', reason: 'Khớp vai trò' },
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

import { StartHerePage } from '../pages/StartHerePage';

describe('StartHerePage (S11) — bắt đầu từ đâu', () => {
  it('liệt kê lộ trình có đánh số: bắt buộc trước rồi gợi ý', async () => {
    render(
      <MemoryRouter>
        <StartHerePage />
      </MemoryRouter>,
    );

    expect(await screen.findByText('Quy trình QA')).toBeInTheDocument();
    expect(screen.getByText('Hướng dẫn Test Case')).toBeInTheDocument();
    // Số thứ tự lộ trình: must_read #1, suggested tiếp theo #2.
    expect(screen.getByText('1')).toBeInTheDocument();
    expect(screen.getByText('2')).toBeInTheDocument();
  });
});
