import { describe, expect, it, vi } from 'vitest';
import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import type { Acknowledgement, DocumentDetail, DocumentVersion } from '../api/types';

// vi.hoisted: fixtures dùng được cả trong vi.mock factory (bị hoist) lẫn assertion.
const fx = vi.hoisted(() => {
  const DOC = {
    doc_id: 'QA-PROC-005',
    title: 'Quy trình Đảm bảo Chất lượng Phần mềm',
    type: 'Process',
    effective_version: '2.1',
    status: 'Effective',
    tags: ['qa', 'quality-gate'],
    mandatory_ack: true,
    is_effective: true,
    content_html: '<h2>Quy trình 8 bước</h2><p>Nội dung mẫu</p><script>alert(1)</script>',
  };
  const VERSIONS = [
    { id: 'eff-uuid', doc_id: 'QA-PROC-005', version: '2.1', status: 'Effective', effective_date: '2026-06-15' },
  ];
  const ACK = { id: 'ack-1', version_id: 'eff-uuid', acked_at: '2026-07-11T09:30:00Z' };
  return { DOC, VERSIONS, ACK };
});

vi.mock('../api/client', () => ({
  ApiError: class ApiError extends Error {
    status = 0;
    code = 'X';
  },
  getDocument: vi.fn(async (): Promise<DocumentDetail> => fx.DOC as DocumentDetail),
  listVersions: vi.fn(async (): Promise<DocumentVersion[]> => fx.VERSIONS as DocumentVersion[]),
  acknowledgeVersion: vi.fn(async (): Promise<Acknowledgement> => fx.ACK as Acknowledgement),
}));

// Import SAU khi vi.mock được khai báo.
import { DocumentView } from '../features/documents/DocumentView';
import { ToastProvider } from '../components/Toast';
import * as client from '../api/client';

describe('DocumentView (S4 vertical slice)', () => {
  it('render tiêu đề, StatusBadge Effective và nội dung đã sanitize', async () => {
    render(<DocumentView docId="QA-PROC-005" />);

    expect(await screen.findByText(fx.DOC.title)).toBeInTheDocument();
    expect(screen.getByText('Đang áp dụng')).toBeInTheDocument();
    expect(screen.getByText('QA-PROC-005')).toBeInTheDocument();

    const content = screen.getByTestId('doc-content');
    expect(content.innerHTML).toContain('Quy trình 8 bước');
    // XSS: thẻ <script> phải bị loại bỏ sau sanitize.
    expect(content.innerHTML).not.toContain('<script>');
  });

  it('tài liệu mandatory_ack hiển thị prompt và ghi nhận khi bấm xác nhận', async () => {
    render(
      <ToastProvider>
        <DocumentView docId="QA-PROC-005" />
      </ToastProvider>,
    );

    const ackBtn = await screen.findByRole('button', { name: /Tôi đã đọc/i });
    fireEvent.click(ackBtn);

    await waitFor(() => expect(client.acknowledgeVersion).toHaveBeenCalledTimes(1));
    // Sau khi ack, prompt (nút) biến mất và text done xuất hiện (ở nút done + toast).
    const dones = await screen.findAllByText('Đã xác nhận đọc tài liệu');
    expect(dones.length).toBeGreaterThanOrEqual(1);
    expect(screen.queryByRole('button', { name: /Tôi đã đọc/i })).not.toBeInTheDocument();
  });
});
