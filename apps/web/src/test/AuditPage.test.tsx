import { describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import type { AuditEntry } from '../api/types';

const h = vi.hoisted(() => ({
  rows: [
    { id: 'au-1', actor: 'qa.lead@fpt', action: 'version.published 2.1 eff=2026-06-15', document_id: null, at: '2026-06-14T08:12:00Z' },
    { id: 'au-3', actor: 'system', action: 'version.auto_effective 2.1', document_id: null, at: '2026-06-15T00:00:05Z' },
  ] as AuditEntry[],
}));

vi.mock('../api/client', () => ({
  ApiError: class ApiError extends Error {
    status = 0;
    code = 'X';
  },
  listAudit: vi.fn(async (): Promise<AuditEntry[]> => h.rows),
}));

import { AuditPage } from '../pages/AuditPage';

describe('AuditPage (S16) — nhật ký kiểm toán', () => {
  it('render bảng audit với actor + action; system là badge', async () => {
    render(<AuditPage />);

    expect(await screen.findByText('qa.lead@fpt')).toBeInTheDocument();
    expect(screen.getByText('version.published 2.1 eff=2026-06-15')).toBeInTheDocument();
    // Hành động hệ thống hiển thị actor "system".
    expect(screen.getByText('system')).toBeInTheDocument();
    expect(screen.getByText('version.auto_effective 2.1')).toBeInTheDocument();
  });
});
