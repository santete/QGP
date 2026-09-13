import { describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import type { AccessReport, ComplianceReport, FeedbackReport, IssuanceReport } from '../api/types';

const h = vi.hoisted(() => ({
  issuance: {
    total_documents: 12,
    effective_count: 9,
    by_type: { Process: 4, Policy: 3 },
    by_version_status: { Effective: 9, Draft: 3 },
    issued_this_month: 2,
    overdue_review: 1,
    due_soon_review: 3,
    with_change_summary_percent: 93,
  } as IssuanceReport,
  compliance: {
    mandatory_docs: 2,
    fully_compliant_docs: 1,
    items: [
      { doc_id: 'QA-PROC-005', title: 'Quy trình QA', effective_version: '2.1', audience_count: 12, acked_count: 12, percent: 100, not_read: [] },
      { doc_id: 'QA-POL-001', title: 'Chính sách Chất lượng', effective_version: '1.0', audience_count: 12, acked_count: 8, percent: 67, not_read: ['bao@fpt'] },
    ],
  } as ComplianceReport,
  feedback: {
    total: 14,
    by_status: { New: 3, Resolved: 7 },
    avg_resolution_hours: 18.5,
    resolved_count: 8,
  } as FeedbackReport,
  access: {
    effective_with_zero_ack: 1,
    zero_ack_docs: [{ doc_id: 'QA-TMPL-004', title: 'Mẫu Test Plan', ack_count: 0 }],
    top_engaged: [{ doc_id: 'QA-PROC-005', title: 'Quy trình QA (đọc nhiều)', ack_count: 42 }],
    note: 'Proxy theo lượt xác nhận đọc — chưa có dữ liệu lượt xem.',
  } as AccessReport,
}));

vi.mock('../api/client', () => ({
  ApiError: class ApiError extends Error {
    status = 0;
    code = 'X';
  },
  getIssuanceReport: vi.fn(async (): Promise<IssuanceReport> => h.issuance),
  getComplianceReport: vi.fn(async (): Promise<ComplianceReport> => h.compliance),
  getFeedbackReport: vi.fn(async (): Promise<FeedbackReport> => h.feedback),
  getAccessReport: vi.fn(async (): Promise<AccessReport> => h.access),
}));

import { ReportsPage } from '../pages/ReportsPage';

describe('ReportsPage (S15) — báo cáo quản trị', () => {
  it('render issuance + access + feedback + compliance', async () => {
    render(
      <MemoryRouter>
        <ReportsPage />
      </MemoryRouter>,
    );

    // Issuance stats.
    expect(await screen.findByText('93%')).toBeInTheDocument(); // with_change_summary
    // Feedback (RPT-F-05).
    expect(screen.getByText('18.5')).toBeInTheDocument(); // avg_resolution_hours
    // Access (RPT-F-03) — proxy note + top engaged.
    expect(screen.getByText(/Proxy theo lượt xác nhận/)).toBeInTheDocument();
    expect(screen.getByText('Quy trình QA (đọc nhiều)')).toBeInTheDocument();
    // Compliance rows.
    expect(screen.getByText('12/12 · 100%')).toBeInTheDocument();
    expect(screen.getByText('8/12 · 67%')).toBeInTheDocument();
    expect(screen.getByText(/bao@fpt/)).toBeInTheDocument();
  });
});
