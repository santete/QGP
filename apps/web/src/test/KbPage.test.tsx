import { describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import type { DocumentSummary } from '../api/types';

const h = vi.hoisted(() => ({
  rows: [
    { doc_id: 'QA-PROC-005', title: 'Quy trình QA', type: 'Process', effective_version: '2.1', status: 'Effective', tags: [] },
    { doc_id: 'QA-POL-003', title: 'Chính sách Bảo mật', type: 'Policy', effective_version: '1.0', status: 'Effective', tags: [] },
    { doc_id: 'QA-WI-009', title: 'Bản nháp', type: 'Work Instruction', effective_version: null, status: 'Draft', tags: [] },
  ] as DocumentSummary[],
}));

vi.mock('../api/client', () => ({
  ApiError: class ApiError extends Error {
    status = 0;
    code = 'X';
  },
  listDocuments: vi.fn(async (): Promise<DocumentSummary[]> => h.rows),
}));

import { KbPage } from '../pages/KbPage';

describe('KbPage (S10) — kho tri thức', () => {
  it('chỉ hiển thị tài liệu Effective, nhóm theo loại; Draft bị loại', async () => {
    render(
      <MemoryRouter>
        <KbPage />
      </MemoryRouter>,
    );

    expect(await screen.findByText('Quy trình QA')).toBeInTheDocument();
    expect(screen.getByText('Chính sách Bảo mật')).toBeInTheDocument();
    // Nhóm theo loại.
    expect(screen.getByText('Process')).toBeInTheDocument();
    expect(screen.getByText('Policy')).toBeInTheDocument();
    // Draft KHÔNG hiển thị (BR-06 — chỉ Effective).
    expect(screen.queryByText('Bản nháp')).not.toBeInTheDocument();
  });
});
