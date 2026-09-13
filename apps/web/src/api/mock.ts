/**
 * Mock API cho dev/vertical-slice (VITE_USE_MOCK=1) — không cần BE thật.
 * Fixture bám đúng shape openapi.yaml (DocumentDetail/DocumentVersion/Acknowledgement)
 * và 11 Business Rule (single Effective, Approved≠Effective...).
 * Nội dung mẫu: quy trình QA 8-step + Quality Gate (seed corpus dev).
 */
import { ApiError } from './client';
import type {
  Acknowledgement,
  AuditEntry,
  CreateDocumentRequest,
  CreateFeedbackRequest,
  DiffResult,
  Feedback,
  FeedbackStatus,
  DocumentDetail,
  DocumentSummary,
  DocumentVersion,
  AccessReport,
  ComplianceReport,
  FeedbackReport,
  IssuanceReport,
  Notification,
  NotificationList,
  Onboarding,
  PublishRequest,
  PublishResult,
  Recommendations,
  ReviewQueueItem,
  SearchHit,
  Subscription,
  SubscriptionStatus,
  VersionContent,
  RbacMatrix,
  AdminUser,
  AdminTag,
  AdminDocType,
  DocAudienceItem,
} from './types';

const NET = 120; // giả lập độ trễ mạng (ms)
const delay = <T>(v: T): Promise<T> => new Promise((r) => setTimeout(() => r(v), NET));

const EFFECTIVE_VERSION_ID = '3f5b9c1e-2a4d-4e6f-8a1b-9c0d1e2f3a4b';

const CONTENT_HTML = `
<h1>Quy trình Đảm bảo Chất lượng Phần mềm</h1>
<p>Tài liệu mô tả quy trình QA 8 bước áp dụng cho mọi dự án phần mềm nội bộ,
kèm hai chốt kiểm soát chất lượng (Quality Gate).</p>
<h2>Phạm vi</h2>
<p>Áp dụng cho tất cả nhóm phát triển thuộc khối Công nghệ.</p>
<h2>Quy trình 8 bước</h2>
<ol>
  <li>Tiếp nhận &amp; phân tích yêu cầu</li>
  <li>Lập kế hoạch kiểm thử</li>
  <li>Thiết kế test case</li>
  <li>Chuẩn bị môi trường &amp; dữ liệu</li>
  <li>Thực thi kiểm thử</li>
  <li>Ghi nhận &amp; theo dõi lỗi</li>
  <li>Kiểm thử hồi quy</li>
  <li>Báo cáo &amp; nghiệm thu</li>
</ol>
<h2>Quality Gate</h2>
<blockquote>
  <p><strong>QG1</strong> — Hoàn tất trước khi vào giai đoạn phát triển:
  tiêu chí chấp nhận rõ ràng, test plan được duyệt.</p>
  <p><strong>QG2</strong> — Hoàn tất trước khi phát hành:
  0 lỗi nghiêm trọng, độ phủ kiểm thử &ge; 80%.</p>
</blockquote>
`.trim();

const DOC: DocumentDetail = {
  doc_id: 'QA-PROC-005',
  title: 'Quy trình Đảm bảo Chất lượng Phần mềm',
  type: 'Process',
  classification: 'Internal',
  effective_version: '2.1',
  status: 'Effective',
  tags: ['qa', 'quy-trinh', 'quality-gate'],
  mandatory_ack: true,
  next_review_date: '2027-01-15',
  audience_roles: ['Engineer', 'QA', 'TeamLead'],
  is_effective: true,
  badge: null,
  effective_link: null,
  content_html: CONTENT_HTML,
};

const VERSIONS: DocumentVersion[] = [
  {
    id: EFFECTIVE_VERSION_ID,
    doc_id: 'QA-PROC-005',
    version: '2.1',
    status: 'Effective',
    issue_date: '2026-06-01',
    effective_date: '2026-06-15',
    change_summary: 'Cập nhật tiêu chí Quality Gate 1',
    created_at: '2026-06-01T03:00:00Z',
  },
  {
    id: '1a2b3c4d-5e6f-4a7b-8c9d-0e1f2a3b4c5d',
    doc_id: 'QA-PROC-005',
    version: '2.0',
    status: 'Superseded',
    issue_date: '2025-12-01',
    effective_date: '2025-12-15',
    change_summary: 'Ban hành quy trình 8 bước',
    created_at: '2025-12-01T03:00:00Z',
  },
];

const SUMMARIES: DocumentSummary[] = [
  {
    doc_id: DOC.doc_id,
    title: DOC.title,
    type: DOC.type,
    classification: DOC.classification,
    effective_version: DOC.effective_version,
    status: DOC.status,
    tags: DOC.tags,
  },
];

export function mockListDocuments(q?: string): Promise<DocumentSummary[]> {
  const term = q?.trim().toLowerCase();
  const rows = term
    ? SUMMARIES.filter((s) => s.title.toLowerCase().includes(term) || s.doc_id.toLowerCase().includes(term))
    : SUMMARIES;
  return delay(rows);
}

// Kết quả search mẫu (S3) — chỉ tài liệu Effective (BR-06), có snippet + score.
const SEARCH_HITS: SearchHit[] = [
  {
    doc_id: 'QA-PROC-005',
    version: '2.1',
    title: 'Quy trình Đảm bảo Chất lượng Phần mềm',
    snippet: 'Tài liệu mô tả quy trình QA 8 bước áp dụng cho mọi dự án phần mềm nội bộ, kèm hai chốt kiểm soát chất lượng (Quality Gate)…',
    score: 0.98,
  },
];

export function mockSearchDocuments(q?: string, _type?: string, _tag?: string): Promise<SearchHit[]> {
  const term = q?.trim().toLowerCase();
  const rows = term
    ? SEARCH_HITS.filter(
        (h) => h.title!.toLowerCase().includes(term) || h.doc_id!.toLowerCase().includes(term) || h.snippet!.toLowerCase().includes(term),
      )
    : SEARCH_HITS;
  return delay(rows);
}

// Gợi ý REC mẫu (S2/S11, BR-11) — reason explainable, nhóm bắt buộc + gợi ý.
const RECOMMENDATIONS: Recommendations = {
  must_read: [
    {
      doc_id: 'QA-PROC-005',
      version: '2.1',
      title: 'Quy trình Đảm bảo Chất lượng Phần mềm',
      effective_date: '2026-06-15',
      reason: 'Bắt buộc',
    },
  ],
  suggested: [
    {
      doc_id: 'QA-GUIDE-002',
      version: '1.3',
      title: 'Hướng dẫn viết Test Case hiệu quả',
      effective_date: '2026-07-20',
      reason: 'Vừa cập nhật',
    },
    {
      doc_id: 'QA-POL-001',
      version: '1.0',
      title: 'Chính sách Chất lượng',
      effective_date: '2026-01-10',
      reason: 'Khớp vai trò',
    },
  ],
};

export function mockGetRecommendations(): Promise<Recommendations> {
  return delay(RECOMMENDATIONS);
}

// Lộ trình onboarding mẫu (S12) — 1 đã đọc / 2 → 50%.
const ONBOARDING: Onboarding = {
  title: 'Nhập môn Chất lượng cho kỹ sư mới',
  roles: ['READER'],
  progress: { total: 2, read: 1, percent: 50 },
  items: [
    { doc_id: 'QA-PROC-005', title: 'Quy trình Đảm bảo Chất lượng Phần mềm', seq: 1, mandatory: true, effective_version: '2.1', acked: true },
    { doc_id: 'QA-POL-001', title: 'Chính sách Chất lượng', seq: 2, mandatory: true, effective_version: '1.0', acked: false },
  ],
};

export function mockGetOnboarding(): Promise<Onboarding> {
  return delay(ONBOARDING);
}

// Báo cáo mẫu (S15).
const ISSUANCE_REPORT: IssuanceReport = {
  total_documents: 12,
  effective_count: 9,
  by_type: { Process: 4, Policy: 3, Procedure: 2, 'Work Instruction': 2, Template: 1 },
  by_version_status: { Draft: 3, InReview: 1, Approved: 1, Effective: 9, Superseded: 5 },
  issued_this_month: 2,
  overdue_review: 1,
  due_soon_review: 3,
  with_change_summary_percent: 93,
};
const COMPLIANCE_REPORT: ComplianceReport = {
  mandatory_docs: 2,
  fully_compliant_docs: 1,
  items: [
    { doc_id: 'QA-PROC-005', title: 'Quy trình Đảm bảo Chất lượng Phần mềm', effective_version: '2.1', audience_count: 12, acked_count: 12, percent: 100, not_read: [] },
    { doc_id: 'QA-POL-001', title: 'Chính sách Chất lượng', effective_version: '1.0', audience_count: 12, acked_count: 8, percent: 67, not_read: ['bao@fpt', 'chi@fpt', 'dung@fpt', 'em@fpt'] },
  ],
};

export function mockGetIssuanceReport(): Promise<IssuanceReport> {
  return delay(ISSUANCE_REPORT);
}

export function mockGetComplianceReport(): Promise<ComplianceReport> {
  return delay(COMPLIANCE_REPORT);
}

const FEEDBACK_REPORT: FeedbackReport = {
  total: 14,
  by_status: { New: 3, Triaged: 2, InProgress: 1, Resolved: 7, Rejected: 1 },
  avg_resolution_hours: 18.5,
  resolved_count: 8,
};
const ACCESS_REPORT: AccessReport = {
  effective_with_zero_ack: 2,
  zero_ack_docs: [
    { doc_id: 'QA-TMPL-004', title: 'Mẫu Test Plan', ack_count: 0 },
    { doc_id: 'QA-WI-011', title: 'Hướng dẫn cấu hình CI', ack_count: 0 },
  ],
  top_engaged: [
    { doc_id: 'QA-PROC-005', title: 'Quy trình Đảm bảo Chất lượng Phần mềm', ack_count: 42 },
    { doc_id: 'QA-POL-001', title: 'Chính sách Chất lượng', ack_count: 27 },
  ],
  note: 'Proxy theo lượt xác nhận đọc — chưa có dữ liệu lượt xem/tra cứu (cần event tracking, P3).',
};

export function mockGetFeedbackReport(): Promise<FeedbackReport> {
  return delay(FEEDBACK_REPORT);
}

export function mockGetAccessReport(): Promise<AccessReport> {
  return delay(ACCESS_REPORT);
}

// Thông báo mẫu (S19) — stateful để mark-read đổi trạng thái.
const NOTIFICATIONS: Notification[] = [
  { id: 'n1', type: 'doc_effective', doc_id: 'QA-PROC-005', title: 'Tài liệu QA-PROC-005 đã ban hành bản 2.1 — vui lòng đọc & xác nhận.', read: false, created_at: '2026-07-25T09:00:00Z' },
  { id: 'n2', type: 'doc_effective', doc_id: 'QA-POL-001', title: 'Tài liệu QA-POL-001 đã ban hành bản 1.0 — vui lòng đọc & xác nhận.', read: false, created_at: '2026-07-20T09:00:00Z' },
  { id: 'n3', type: 'doc_effective', doc_id: 'QA-GUIDE-002', title: 'Tài liệu QA-GUIDE-002 đã ban hành bản 1.3.', read: true, created_at: '2026-07-10T09:00:00Z' },
];

function unreadCount(): number {
  return NOTIFICATIONS.filter((n) => !n.read).length;
}

export function mockGetNotifications(unreadOnly = false): Promise<NotificationList> {
  const data = (unreadOnly ? NOTIFICATIONS.filter((n) => !n.read) : NOTIFICATIONS)
    .slice()
    .sort((a, b) => Number(a.read) - Number(b.read));
  return delay({ data, unread_count: unreadCount() });
}

export function mockMarkNotificationRead(id: string): Promise<Notification> {
  const n = NOTIFICATIONS.find((x) => x.id === id);
  if (!n) return Promise.reject(new ApiError(404, 'NOTIFICATION_NOT_FOUND', 'Không tìm thấy thông báo'));
  n.read = true;
  return delay({ ...n });
}

export function mockMarkAllNotificationsRead(): Promise<{ updated: number }> {
  const updated = NOTIFICATIONS.filter((n) => !n.read).length;
  NOTIFICATIONS.forEach((n) => (n.read = true));
  return delay({ updated });
}

// Theo dõi tài liệu (S19) — stateful set.
const SUBSCRIPTIONS = new Map<string, Subscription>([
  ['QA-POL-001', { doc_id: 'QA-POL-001', title: 'Chính sách Chất lượng', subscribed_at: '2026-07-18T09:00:00Z' }],
]);

export function mockListSubscriptions(): Promise<Subscription[]> {
  return delay([...SUBSCRIPTIONS.values()]);
}

export function mockSubscribeDocument(docId: string): Promise<SubscriptionStatus> {
  if (!SUBSCRIPTIONS.has(docId))
    SUBSCRIPTIONS.set(docId, { doc_id: docId, title: docId, subscribed_at: '2026-07-26T00:00:00Z' });
  return delay({ doc_id: docId, subscribed: true });
}

export function mockUnsubscribeDocument(docId: string): Promise<SubscriptionStatus> {
  SUBSCRIPTIONS.delete(docId);
  return delay({ doc_id: docId, subscribed: false });
}

export function mockGetDocument(docId: string): Promise<DocumentDetail> {
  if (docId !== DOC.doc_id) {
    return Promise.reject(new ApiError(404, 'DOCUMENT_NOT_FOUND', `Không tìm thấy tài liệu ${docId}`));
  }
  return delay(DOC);
}

// Feedback in-memory (S13/S14).
let FEEDBACK: Feedback[] = [
  {
    id: 'fb-1',
    version_id: 'v-21',
    doc_id: 'QA-PROC-005',
    version: '2.1',
    category: 'content_error',
    status: 'New',
    body: 'Bước 5 ghi sai thứ tự so với thực tế.',
    created_at: '2026-07-21T04:10:00Z',
  },
  {
    id: 'fb-2',
    version_id: 'v-21',
    doc_id: 'QA-PROC-005',
    version: '2.1',
    category: 'unclear',
    status: 'Triaged',
    body: 'Tiêu chí QG2 chưa rõ ngưỡng độ phủ.',
    created_at: '2026-07-19T02:00:00Z',
  },
];

export function mockCreateFeedback(req: CreateFeedbackRequest): Promise<Feedback> {
  const fb: Feedback = {
    id: `fb-${req.doc_id}-${FEEDBACK.length + 1}`,
    version_id: `mock-${req.doc_id}-${req.version}`,
    doc_id: req.doc_id,
    version: req.version,
    category: req.category,
    status: 'New',
    body: req.body ?? null,
    created_at: '2026-07-25T00:00:00Z',
  };
  FEEDBACK = [fb, ...FEEDBACK];
  return delay(fb);
}

export function mockListFeedback(): Promise<Feedback[]> {
  return delay([...FEEDBACK]);
}

export function mockTriageFeedback(id: string, status: FeedbackStatus): Promise<Feedback> {
  const fb = FEEDBACK.find((x) => x.id === id);
  if (!fb) return Promise.reject(new ApiError(404, 'FEEDBACK_NOT_FOUND', 'Không tìm thấy feedback'));
  fb.status = status;
  return delay({ ...fb });
}

export function mockListAudit(): Promise<AuditEntry[]> {
  return delay([
    { id: 'au-1', actor: 'qa.lead@fpt', action: 'version.published 2.1 eff=2026-06-15', document_id: null, at: '2026-06-14T08:12:00Z' },
    { id: 'au-2', actor: 'author@fpt', action: 'version.submitted 2.1', document_id: null, at: '2026-06-10T03:40:00Z' },
    { id: 'au-3', actor: 'system', action: 'version.auto_effective 2.1', document_id: null, at: '2026-06-15T00:00:05Z' },
    { id: 'au-4', actor: 'author@fpt', action: 'document.created', document_id: null, at: '2026-05-30T02:00:00Z' },
  ]);
}

export function mockGetDiff(_docId: string, from: string, to: string): Promise<DiffResult> {
  return delay({
    from,
    to,
    diff: [
      `--- ${_docId}@${from}`,
      `+++ ${_docId}@${to}`,
      '@@ -1,6 +1,7 @@',
      ' # Quy trình Đảm bảo Chất lượng Phần mềm',
      ' ',
      ' ## Quality Gate',
      '-QG1 — tiêu chí chấp nhận rõ ràng.',
      '+QG1 — tiêu chí chấp nhận rõ ràng, test plan được duyệt.',
      '+QG2 — 0 lỗi nghiêm trọng, độ phủ ≥ 80%.',
      ' ',
    ].join('\n'),
    change_summary: 'Cập nhật tiêu chí Quality Gate 1',
  });
}

export function mockListVersions(docId: string): Promise<DocumentVersion[]> {
  const draft = mockListVersionsForDraft(docId);
  if (draft) return delay(draft);
  if (docId !== DOC.doc_id) {
    return Promise.reject(new ApiError(404, 'DOCUMENT_NOT_FOUND', `Không tìm thấy tài liệu ${docId}`));
  }
  return delay(VERSIONS);
}

// Hàng đợi duyệt mẫu (S9) — in-memory, approve/reject sẽ gỡ khỏi hàng đợi.
let REVIEW_QUEUE: ReviewQueueItem[] = [
  {
    version_id: '7c2f0a11-1111-4d22-8e33-aa0011223344',
    doc_id: 'QA-PROC-007',
    title: 'Quy trình Quản lý Rủi ro Dự án',
    version: '1.3',
    submitted_at: '2026-07-20T02:15:00Z',
  },
  {
    version_id: '9b1d2c33-2222-4a44-9f55-bb2233445566',
    doc_id: 'QA-WI-012',
    title: 'Hướng dẫn Kiểm thử Hồi quy Tự động',
    version: '2.0',
    submitted_at: '2026-07-22T07:40:00Z',
  },
];

// Hàng đợi chờ ban hành (S8) — Approved. Approve sẽ chuyển item từ REVIEW_QUEUE sang đây.
let APPROVED_QUEUE: ReviewQueueItem[] = [
  {
    version_id: 'c3d4e5f6-3333-4b55-8c66-cc3344556677',
    doc_id: 'QA-POL-003',
    title: 'Chính sách Bảo mật Thông tin',
    version: '1.0',
    submitted_at: '2026-07-18T09:00:00Z',
  },
];

export function mockListReviewQueue(status: 'InReview' | 'Approved' = 'InReview'): Promise<ReviewQueueItem[]> {
  return delay(status === 'Approved' ? [...APPROVED_QUEUE] : [...REVIEW_QUEUE]);
}

export function mockApproveVersion(
  versionId: string,
  _decision: 'approve' | 'reject',
): Promise<DocumentVersion> {
  const item = REVIEW_QUEUE.find((x) => x.version_id === versionId);
  if (!item) return Promise.reject(new ApiError(404, 'VERSION_NOT_FOUND', 'Không tìm thấy version'));
  REVIEW_QUEUE = REVIEW_QUEUE.filter((x) => x.version_id !== versionId);
  if (_decision === 'approve') APPROVED_QUEUE = [...APPROVED_QUEUE, item]; // chuyển sang chờ ban hành
  return delay({
    id: item.version_id,
    doc_id: item.doc_id,
    version: item.version,
    status: _decision === 'approve' ? 'Approved' : 'Draft',
    issue_date: null,
    effective_date: null,
    created_at: item.submitted_at,
  });
}

export function mockPublishVersion(versionId: string, req: PublishRequest): Promise<PublishResult> {
  const item = APPROVED_QUEUE.find((x) => x.version_id === versionId);
  if (!item) return Promise.reject(new ApiError(404, 'VERSION_NOT_FOUND', 'Không tìm thấy version'));
  APPROVED_QUEUE = APPROVED_QUEUE.filter((x) => x.version_id !== versionId);
  return delay({
    version_id: item.version_id,
    doc_id: item.doc_id,
    version: item.version,
    status: 'Published',
    effective_date: req.effective_date,
    badge: `Sắp áp dụng từ ${req.effective_date}`,
  });
}

// Editor S7 (mock) — draft in-memory tạo bởi mockCreateDocument, sửa/gửi duyệt được.
interface MockDraft {
  detail: DocumentDetail;
  version: DocumentVersion;
}
const MOCK_DRAFTS: Record<string, MockDraft> = {}; // key = doc_id

export function mockCreateDocument(req: CreateDocumentRequest): Promise<DocumentDetail> {
  const versionId = `draft-${req.doc_id}`;
  const detail: DocumentDetail = {
    doc_id: req.doc_id,
    title: req.title,
    type: req.type,
    classification: req.classification,
    effective_version: null,
    status: 'Draft',
    tags: req.tags ?? [],
    mandatory_ack: req.mandatory_ack ?? false,
    next_review_date: req.next_review_date ?? null,
    audience_roles: req.audience_roles ?? [],
    is_effective: false,
    badge: null,
    effective_link: null,
    content_html: '',
  };
  MOCK_DRAFTS[req.doc_id] = {
    detail,
    version: {
      id: versionId,
      doc_id: req.doc_id,
      version: '1.0',
      status: 'Draft',
      issue_date: null,
      effective_date: null,
      change_summary: undefined,
      created_at: '2026-07-25T00:00:00Z',
    },
  };
  // Lưu content ban đầu.
  DRAFT_CONTENT[versionId] = req.content_markdown ?? '';
  return delay(detail);
}

const DRAFT_CONTENT: Record<string, string> = {};

function findDraftByVersion(versionId: string): MockDraft | undefined {
  return Object.values(MOCK_DRAFTS).find((d) => d.version.id === versionId);
}

export function mockGetVersionContent(versionId: string): Promise<VersionContent> {
  const d = findDraftByVersion(versionId);
  if (!d) return Promise.reject(new ApiError(404, 'VERSION_NOT_FOUND', 'Không tìm thấy version'));
  return delay({
    version_id: versionId,
    doc_id: d.detail.doc_id,
    version: d.version.version,
    status: d.version.status,
    content_markdown: DRAFT_CONTENT[versionId] ?? '',
  });
}

export function mockUpdateVersionContent(versionId: string, contentMarkdown: string): Promise<VersionContent> {
  const d = findDraftByVersion(versionId);
  if (!d) return Promise.reject(new ApiError(404, 'VERSION_NOT_FOUND', 'Không tìm thấy version'));
  if (d.version.status !== 'Draft')
    return Promise.reject(new ApiError(409, 'VERSION_IMMUTABLE', 'Bản đã ban hành là bất biến (BR-03)'));
  DRAFT_CONTENT[versionId] = contentMarkdown;
  return delay({
    version_id: versionId,
    doc_id: d.detail.doc_id,
    version: d.version.version,
    status: d.version.status,
    content_markdown: contentMarkdown,
  });
}

export function mockSubmitVersion(versionId: string): Promise<DocumentVersion> {
  const d = findDraftByVersion(versionId);
  if (!d) return Promise.reject(new ApiError(404, 'VERSION_NOT_FOUND', 'Không tìm thấy version'));
  d.version = { ...d.version, status: 'InReview' };
  return delay(d.version);
}

export function mockListVersionsForDraft(docId: string): DocumentVersion[] | null {
  return MOCK_DRAFTS[docId] ? [MOCK_DRAFTS[docId].version] : null;
}

export function mockAcknowledgeVersion(versionId: string): Promise<Acknowledgement> {
  return delay({
    id: 'ack-00000000-0000-4000-8000-000000000001',
    version_id: versionId,
    acked_at: '2026-07-11T09:30:00Z',
  });
}

// ── Admin / S17 (ADM-F-01/02) — mock cho dev VITE_USE_MOCK (không bền, đủ demo) ──
export function mockGetRbac(): Promise<RbacMatrix> {
  return delay({
    roles: [
      { code: 'READER', name: 'Reader' },
      { code: 'CONTRIBUTOR', name: 'Contributor' },
      { code: 'AUTHOR', name: 'Author' },
      { code: 'APPROVER', name: 'Approver' },
      { code: 'QA_LEAD', name: 'QA Lead' },
      { code: 'ADMIN', name: 'Admin' },
    ],
    policies: [
      { policy: 'doc.read', roles: ['READER', 'CONTRIBUTOR', 'AUTHOR', 'APPROVER', 'QA_LEAD', 'ADMIN'] },
      { policy: 'kb.contribute', roles: ['CONTRIBUTOR', 'AUTHOR', 'APPROVER', 'QA_LEAD', 'ADMIN'] },
      { policy: 'doc.author', roles: ['AUTHOR', 'APPROVER', 'QA_LEAD'] },
      { policy: 'doc.approve', roles: ['APPROVER', 'QA_LEAD'] },
      { policy: 'admin.config', roles: ['QA_LEAD', 'ADMIN'] },
    ],
  });
}

export function mockListAdminUsers(): Promise<AdminUser[]> {
  return delay([
    { sub: 'author1', display_name: 'author1', email: null, roles: ['AUTHOR', 'READER'] },
    { sub: 'qa1', display_name: 'qa1', email: null, roles: ['QA_LEAD'] },
    { sub: 'admin1', display_name: 'admin1', email: null, roles: ['ADMIN'] },
  ]);
}

export function mockListTags(): Promise<AdminTag[]> {
  return delay([
    { id: 'tag-1', slug: 'qa', name: 'qa', doc_count: 3 },
    { id: 'tag-2', slug: 'quality-gate', name: 'quality-gate', doc_count: 1 },
  ]);
}
export function mockCreateTag(slug: string, name: string): Promise<AdminTag> {
  return delay({ id: `tag-${slug}`, slug, name: name || slug, doc_count: 0 });
}
export function mockUpdateTag(id: string, body: { slug?: string; name?: string }): Promise<AdminTag> {
  return delay({ id, slug: body.slug ?? 'qa', name: body.name ?? 'qa', doc_count: 0 });
}
export function mockDeleteTag(_id: string): Promise<void> {
  return delay(undefined);
}

const MOCK_DOC_TYPES: AdminDocType[] = [
  { id: 'dt-1', code: 'Policy', label: 'Policy', seq: 1, active: true, doc_count: 2 },
  { id: 'dt-2', code: 'Process', label: 'Process', seq: 2, active: true, doc_count: 5 },
  { id: 'dt-3', code: 'Procedure', label: 'Procedure', seq: 3, active: true, doc_count: 3 },
  { id: 'dt-4', code: 'Work Instruction', label: 'Work Instruction', seq: 4, active: true, doc_count: 1 },
  { id: 'dt-5', code: 'Template', label: 'Template', seq: 5, active: true, doc_count: 0 },
  { id: 'dt-6', code: 'Checklist', label: 'Checklist', seq: 6, active: true, doc_count: 0 },
  { id: 'dt-7', code: 'Standard', label: 'Standard', seq: 7, active: true, doc_count: 0 },
];
export function mockListDocTypes(): Promise<AdminDocType[]> {
  return delay(MOCK_DOC_TYPES.map((t) => ({ ...t })));
}
export function mockCreateDocType(code: string, label?: string): Promise<AdminDocType> {
  return delay({ id: `dt-${code}`, code, label: label || code, seq: 99, active: true, doc_count: 0 });
}
export function mockUpdateDocType(id: string, body: { label?: string; seq?: number; active?: boolean }): Promise<AdminDocType> {
  const base = MOCK_DOC_TYPES.find((t) => t.id === id) ?? MOCK_DOC_TYPES[0];
  return delay({ ...base, label: body.label ?? base.label, seq: body.seq ?? base.seq, active: body.active ?? base.active });
}
export function mockDeleteDocType(_id: string): Promise<void> {
  return delay(undefined);
}

export function mockGetDocAudience(_docId: string): Promise<DocAudienceItem[]> {
  return delay([{ role: 'READER', reason: 'Khớp vai trò' }]);
}
export function mockSetDocAudience(_docId: string, audience: DocAudienceItem[]): Promise<DocAudienceItem[]> {
  return delay(audience);
}
