import { describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import type { SearchHit } from '../api/types';

const fx = vi.hoisted(() => ({
  rows: [
    {
      doc_id: 'QA-PROC-005',
      version: '2.1',
      title: 'Quy trình Đảm bảo Chất lượng Phần mềm',
      snippet: 'Tài liệu mô tả quy trình QA 8 bước áp dụng cho mọi dự án phần mềm nội bộ…',
      score: 0.98,
    },
  ] as SearchHit[],
}));

vi.mock('../api/client', () => ({
  ApiError: class ApiError extends Error {
    status = 0;
    code = 'X';
  },
  searchDocuments: vi.fn(async (): Promise<SearchHit[]> => fx.rows),
}));

import { DocumentsPage } from '../pages/DocumentsPage';

describe('DocumentsPage (S3) — search full-text', () => {
  it('render kết quả search từ /v1/search với snippet + Effective badge + link chi tiết', async () => {
    render(
      <MemoryRouter>
        <DocumentsPage />
      </MemoryRouter>,
    );

    expect(await screen.findByText('Quy trình Đảm bảo Chất lượng Phần mềm')).toBeInTheDocument();
    expect(screen.getByText('QA-PROC-005')).toBeInTheDocument();
    // Search chỉ index Effective (BR-06) → badge "Đang áp dụng".
    expect(screen.getByText('Đang áp dụng')).toBeInTheDocument();
    // Snippet hiển thị.
    expect(screen.getByText(/quy trình QA 8 bước/i)).toBeInTheDocument();

    const link = screen.getByRole('link', { name: /Quy trình Đảm bảo/i });
    expect(link).toHaveAttribute('href', '/documents/QA-PROC-005');
  });
});
