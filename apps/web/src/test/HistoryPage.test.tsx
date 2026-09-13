import { describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import type { DiffResult, DocumentVersion } from '../api/types';

const h = vi.hoisted(() => ({
  versions: [
    { id: 'v-21', doc_id: 'QA-PROC-005', version: '2.1', status: 'Effective', issue_date: null, effective_date: null, change_summary: 'Cập nhật QG1', created_at: '2026-06-01T00:00:00Z' },
    { id: 'v-20', doc_id: 'QA-PROC-005', version: '2.0', status: 'Superseded', issue_date: null, effective_date: null, change_summary: 'Ban hành đầu', created_at: '2025-12-01T00:00:00Z' },
  ] as DocumentVersion[],
  diff: {
    from: '2.0',
    to: '2.1',
    diff: '--- QA-PROC-005@2.0\n+++ QA-PROC-005@2.1\n@@ -1,2 +1,2 @@\n-QG1 cũ\n+QG1 mới\n',
    change_summary: 'Cập nhật QG1',
  } as DiffResult,
}));

vi.mock('../api/client', () => ({
  ApiError: class ApiError extends Error {
    status = 0;
    code = 'X';
  },
  listVersions: vi.fn(async (): Promise<DocumentVersion[]> => h.versions),
  getDiff: vi.fn(async (): Promise<DiffResult> => h.diff),
}));

import { HistoryPage } from '../pages/HistoryPage';

describe('HistoryPage (S6) — version history + diff', () => {
  it('render danh sách version + unified diff (dòng thêm/xoá)', async () => {
    render(
      <MemoryRouter initialEntries={['/documents/QA-PROC-005/history']}>
        <Routes>
          <Route path="/documents/:docId/history" element={<HistoryPage />} />
        </Routes>
      </MemoryRouter>,
    );

    // Danh sách version (xuất hiện ở cả badge + option select → dùng getAllByText).
    expect((await screen.findAllByText('v2.1')).length).toBeGreaterThan(0);
    expect(screen.getAllByText('v2.0').length).toBeGreaterThan(0);

    // Diff hiển thị dòng thêm (+) và xoá (-).
    expect(await screen.findByText('-QG1 cũ')).toBeInTheDocument();
    expect(screen.getByText('+QG1 mới')).toBeInTheDocument();

    // Tóm tắt thay đổi (hiện ở cả badge version + header diff).
    expect(screen.getAllByText(/Cập nhật QG1/).length).toBeGreaterThan(0);
  });
});
