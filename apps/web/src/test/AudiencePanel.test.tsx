import { beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import type {
  Acknowledgement,
  CreateFeedbackRequest,
  DocumentDetail,
  DocumentVersion,
  DocAudienceItem,
  Feedback,
  Subscription,
  SubscriptionStatus,
} from '../api/types';

// Fixtures + mock fn (hoisted — dùng được trong vi.mock factory bị hoist).
const fx = vi.hoisted(() => {
  const DOC = {
    doc_id: 'QA-PROC-005',
    title: 'Quy trình Đảm bảo Chất lượng Phần mềm',
    type: 'Process',
    effective_version: '2.1',
    status: 'Effective',
    tags: ['qa'],
    mandatory_ack: false,
    is_effective: true,
    content_html: '<p>Nội dung mẫu</p>',
  };
  const VERSIONS = [
    { id: 'eff-uuid', doc_id: 'QA-PROC-005', version: '2.1', status: 'Effective', effective_date: '2026-06-15' },
  ];
  const AUDIENCE: DocAudienceItem[] = [
    { role: 'READER', reason: 'Bắt buộc' },
  ];
  // vi.fn phải nằm trong vi.hoisted để vi.mock factory truy cập được.
  const mockGetDocAudience = vi.fn(async (): Promise<DocAudienceItem[]> => AUDIENCE);
  const mockSetDocAudience = vi.fn(async (_docId: string, audience: DocAudienceItem[]): Promise<DocAudienceItem[]> => audience);
  const mockUseAuth = vi.fn();
  return { DOC, VERSIONS, AUDIENCE, mockGetDocAudience, mockSetDocAudience, mockUseAuth };
});

vi.mock('../api/client', () => ({
  ApiError: class ApiError extends Error {
    status = 0;
    code = 'X';
  },
  getDocument: vi.fn(async (): Promise<DocumentDetail> => fx.DOC as DocumentDetail),
  listVersions: vi.fn(async (): Promise<DocumentVersion[]> => fx.VERSIONS as DocumentVersion[]),
  getDocAudience: fx.mockGetDocAudience,
  setDocAudience: fx.mockSetDocAudience,
  // SubscribeButton + FeedbackButton cũng import từ client — stub để không crash.
  listSubscriptions: vi.fn(async (): Promise<Subscription[]> => []),
  subscribeDocument: vi.fn(async (docId: string): Promise<SubscriptionStatus> => ({ doc_id: docId, subscribed: true })),
  unsubscribeDocument: vi.fn(async (docId: string): Promise<SubscriptionStatus> => ({ doc_id: docId, subscribed: false })),
  createFeedback: vi.fn(async (_req: CreateFeedbackRequest): Promise<Feedback> => ({ id: 'fb-1', doc_id: 'QA-PROC-005', version: '2.1', category: 'content_error', body: '', status: 'New', created_at: '2026-07-01T00:00:00Z' })),
  acknowledgeVersion: vi.fn(async (): Promise<Acknowledgement> => ({ id: 'ack-1', version_id: 'eff-uuid', acked_at: '2026-07-11T09:30:00Z' })),
}));

// Mock auth — role set per test qua fx.mockUseAuth.mockReturnValue.
vi.mock('../auth/AuthContext', () => ({
  useAuth: fx.mockUseAuth,
}));

// Import SAU khi vi.mock được khai báo.
import { DocumentPage } from '../pages/DocumentPage';
import { ToastProvider } from '../components/Toast';
import { strings } from '../i18n/strings';

/** Helper: render DocumentPage với role cụ thể (MemoryRouter + route /documents/:docId). */
function renderPageWithRole(roles: string[]) {
  fx.mockUseAuth.mockReturnValue({
    user: { sub: 'tester', roles },
    isAuthenticated: true,
    hasAnyRole: (rs: string[]) => roles.some((r) => rs.includes(r)),
  });
  render(
    <ToastProvider>
      <MemoryRouter initialEntries={['/documents/QA-PROC-005']}>
        <Routes>
          <Route path="/documents/:docId" element={<DocumentPage />} />
        </Routes>
      </MemoryRouter>
    </ToastProvider>,
  );
}

describe('AudiencePanel (b1c1) — sửa audience per-document', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    fx.mockGetDocAudience.mockResolvedValue(fx.AUDIENCE);
    fx.mockSetDocAudience.mockImplementation(async (_d: string, a: DocAudienceItem[]) => a);
  });

  // ── Test 1: Admin (ADMIN_ROLES) thấy được AudiencePanel ──
  it('hiển thị AudiencePanel khi user có role ADMIN', async () => {
    renderPageWithRole(['ADMIN']);

    // AudiencePanel tải audience — label role (ROLE_LABEL['READER']='Người đọc') + reason phải xuất hiện.
    await waitFor(() => expect(screen.getByText('Người đọc')).toBeInTheDocument());
    expect(screen.getByDisplayValue('Bắt buộc')).toBeInTheDocument();
  });

  // ── Test 2: Người thường (READER) KHÔNG thấy AudiencePanel ──
  it('KHÔNG hiển thị AudiencePanel khi user chỉ có role READER', async () => {
    renderPageWithRole(['READER']);

    // Đợi DocumentView render (tiêu đề xuất hiện).
    expect(await screen.findByText(fx.DOC.title)).toBeInTheDocument();

    // AudiencePanel KHÔNG có (chỉ ADMIN_ROLES mới thấy) — không có title audience.
    expect(screen.queryByText(strings.audience.title)).not.toBeInTheDocument();
  });

  // ── Test 3: Bấm lưu gửi đúng dữ liệu (setDocAudience với docId + DocAudienceItem[]) ──
  it('bấm Lưu gọi setDocAudience với docId và mảng audience đúng shape', async () => {
    renderPageWithRole(['ADMIN']);

    // Đợi audience load — label role xuất hiện.
    await waitFor(() => expect(screen.getByText('Người đọc')).toBeInTheDocument());

    // Bấm nút Lưu.
    const saveBtn = screen.getByRole('button', { name: /Lưu/i });
    fireEvent.click(saveBtn);

    // Verify setDocAudience được gọi với đúng docId + shape DocAudienceItem[].
    await waitFor(() => expect(fx.mockSetDocAudience).toHaveBeenCalledTimes(1));
    expect(fx.mockSetDocAudience).toHaveBeenCalledWith(
      'QA-PROC-005',
      expect.arrayContaining([
        expect.objectContaining({ role: expect.any(String), reason: expect.any(String) }),
      ]),
    );
  });
});
