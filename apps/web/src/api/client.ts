/**
 * Typed fetch wrapper cho qgp-api (/v1). Single source cho enum/field = openapi.yaml.
 * - Gắn Authorization Bearer (OIDC access token; dev dùng mock token).
 * - Verb unsafe gắn Idempotency-Key (UUID) theo openapi.yaml §3.4.
 * - Lỗi HTTP → ApiError mang error.code (ISC Error Code catalog) để UI map sang toast.
 * - VITE_USE_MOCK=1 → route sang mock (chạy vertical slice không cần BE).
 */
import type {
  Acknowledgement,
  ApiErrorBody,
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
import {
  mockAcknowledgeVersion,
  mockApproveVersion,
  mockCreateDocument,
  mockGetDiff,
  mockGetDocument,
  mockGetVersionContent,
  mockListDocuments,
  mockCreateFeedback,
  mockListAudit,
  mockListFeedback,
  mockListReviewQueue,
  mockListVersions,
  mockPublishVersion,
  mockGetRecommendations,
  mockGetOnboarding,
  mockGetIssuanceReport,
  mockGetComplianceReport,
  mockGetFeedbackReport,
  mockGetAccessReport,
  mockGetNotifications,
  mockMarkNotificationRead,
  mockMarkAllNotificationsRead,
  mockListSubscriptions,
  mockSubscribeDocument,
  mockUnsubscribeDocument,
  mockSearchDocuments,
  mockSubmitVersion,
  mockTriageFeedback,
  mockUpdateVersionContent,
  mockGetRbac,
  mockListAdminUsers,
  mockListTags,
  mockCreateTag,
  mockUpdateTag,
  mockDeleteTag,
  mockListDocTypes,
  mockCreateDocType,
  mockUpdateDocType,
  mockDeleteDocType,
  mockGetDocAudience,
  mockSetDocAudience,
} from './mock';

interface Page<T> {
  data: T[];
  next_cursor: string | null;
}

const BASE_URL = import.meta.env.VITE_API_BASE_URL ?? '/v1';
const USE_MOCK = import.meta.env.VITE_USE_MOCK === '1';

/** Lỗi API có mã (error.code) — UI map code → thông điệp/toast. */
export class ApiError extends Error {
  readonly status: number;
  readonly code: string;
  readonly details?: unknown;

  constructor(status: number, code: string, message: string, details?: unknown) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.code = code;
    this.details = details;
  }
}

function authToken(): string | null {
  // Dev: mock token qua env. Prod: OIDC access token lưu sau SSO callback.
  return (
    (typeof localStorage !== 'undefined' && localStorage.getItem('qgp.access_token')) ||
    import.meta.env.VITE_DEV_TOKEN ||
    null
  );
}

function newIdempotencyKey(): string {
  if (typeof crypto !== 'undefined' && 'randomUUID' in crypto) return crypto.randomUUID();
  // Fallback môi trường không có crypto.randomUUID (hiếm).
  return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, (c) => {
    const r = (Math.random() * 16) | 0;
    const v = c === 'x' ? r : (r & 0x3) | 0x8;
    return v.toString(16);
  });
}

interface RequestOptions {
  method?: 'GET' | 'POST' | 'PATCH' | 'PUT' | 'DELETE';
  body?: unknown;
  /** Gắn Idempotency-Key (bắt buộc cho verb unsafe theo contract). */
  idempotent?: boolean;
  signal?: AbortSignal;
}

async function request<T>(path: string, opts: RequestOptions = {}): Promise<T> {
  const { method = 'GET', body, idempotent, signal } = opts;

  const headers: Record<string, string> = { Accept: 'application/json' };
  const token = authToken();
  if (token) headers.Authorization = `Bearer ${token}`;
  if (body !== undefined) headers['Content-Type'] = 'application/json';
  if (idempotent) headers['Idempotency-Key'] = newIdempotencyKey();

  const res = await fetch(`${BASE_URL}${path}`, {
    method,
    headers,
    body: body !== undefined ? JSON.stringify(body) : undefined,
    signal,
  });

  if (res.status === 204) return undefined as T;

  const text = await res.text();
  const parsed: unknown = text ? JSON.parse(text) : null;

  if (!res.ok) {
    const err = (parsed as ApiErrorBody | null)?.error;
    throw new ApiError(
      res.status,
      err?.code ?? `HTTP_${res.status}`,
      err?.message ?? res.statusText ?? 'Đã xảy ra lỗi',
      err?.details,
    );
  }

  return parsed as T;
}

/** GET /documents — danh sách/lọc tài liệu (listDocuments, S3). q lọc theo title/doc_id. */
export function listDocuments(q?: string, signal?: AbortSignal): Promise<DocumentSummary[]> {
  if (USE_MOCK) return mockListDocuments(q);
  const qs = q ? `?q=${encodeURIComponent(q)}` : '';
  return request<Page<DocumentSummary>>(`/documents${qs}`, { signal }).then((p) => p.data);
}

/**
 * GET /search — full-text Meilisearch (chỉ tài liệu Effective — BR-06), trả snippet + score.
 * Dùng cho màn Tài liệu S3 (q rỗng → toàn bộ Effective, ranked).
 */
export function searchDocuments(
  q?: string,
  opts: { type?: string; tag?: string; limit?: number } = {},
  signal?: AbortSignal,
): Promise<SearchHit[]> {
  if (USE_MOCK) return mockSearchDocuments(q, opts.type, opts.tag);
  const params = new URLSearchParams();
  if (q) params.set('q', q);
  if (opts.type) params.set('type', opts.type);
  if (opts.tag) params.set('tag', opts.tag);
  if (opts.limit) params.set('limit', String(opts.limit));
  const qs = params.toString();
  return request<Page<SearchHit>>(`/search${qs ? `?${qs}` : ''}`, { signal }).then((p) => p.data);
}

/** GET /documents/{doc_id} — bản Effective + badge trạng thái (getDocument). */
export function getDocument(docId: string, signal?: AbortSignal): Promise<DocumentDetail> {
  if (USE_MOCK) return mockGetDocument(docId);
  return request<DocumentDetail>(`/documents/${encodeURIComponent(docId)}`, { signal });
}

/** GET /documents/{doc_id}/versions — lịch sử phiên bản (listVersions). */
export function listVersions(docId: string, signal?: AbortSignal): Promise<DocumentVersion[]> {
  if (USE_MOCK) return mockListVersions(docId);
  return request<DocumentVersion[]>(`/documents/${encodeURIComponent(docId)}/versions`, { signal });
}

/** POST /feedback — gửi phản hồi in-context (S13). Server tự gắn version_id. */
export function createFeedback(req: CreateFeedbackRequest): Promise<Feedback> {
  if (USE_MOCK) return mockCreateFeedback(req);
  return request<Feedback>('/feedback', { method: 'POST', body: req, idempotent: true });
}

/** GET /feedback — danh sách để triage (S14). RBAC admin.config. */
export function listFeedback(
  params: { status?: FeedbackStatus; category?: string; docId?: string; limit?: number } = {},
  signal?: AbortSignal,
): Promise<Feedback[]> {
  if (USE_MOCK) return mockListFeedback();
  const p = new URLSearchParams();
  if (params.status) p.set('status', params.status);
  if (params.category) p.set('category', params.category);
  if (params.docId) p.set('doc_id', params.docId);
  if (params.limit) p.set('limit', String(params.limit));
  const qs = p.toString();
  return request<Page<Feedback>>(`/feedback${qs ? `?${qs}` : ''}`, { signal }).then((pg) => pg.data);
}

/** PATCH /feedback/{id} — triage đổi trạng thái (S14). RBAC admin.config. */
export function triageFeedback(id: string, status: FeedbackStatus, note?: string): Promise<Feedback> {
  if (USE_MOCK) return mockTriageFeedback(id, status);
  return request<Feedback>(`/feedback/${encodeURIComponent(id)}`, {
    method: 'PATCH',
    body: { status, note },
    idempotent: true,
  });
}

/** GET /audit — truy vết audit (S16, ADM-F-03). RBAC admin.config. */
export function listAudit(
  params: { documentId?: string; from?: string; to?: string; limit?: number } = {},
  signal?: AbortSignal,
): Promise<AuditEntry[]> {
  if (USE_MOCK) return mockListAudit();
  const p = new URLSearchParams();
  if (params.documentId) p.set('document_id', params.documentId);
  if (params.from) p.set('from', params.from);
  if (params.to) p.set('to', params.to);
  if (params.limit) p.set('limit', String(params.limit));
  const qs = p.toString();
  return request<Page<AuditEntry>>(`/audit${qs ? `?${qs}` : ''}`, { signal }).then((pg) => pg.data);
}

/** GET /recommendations — gợi ý explainable theo role (S2/S11, REC-F-02, BR-11). Nhóm must_read + suggested. */
export function getRecommendations(signal?: AbortSignal): Promise<Recommendations> {
  if (USE_MOCK) return mockGetRecommendations();
  return request<Recommendations>('/recommendations', { signal });
}

/** GET /onboarding — lộ trình học theo role + tiến độ đọc (S12, ONB-F-01/02/04). */
export function getOnboarding(signal?: AbortSignal): Promise<Onboarding> {
  if (USE_MOCK) return mockGetOnboarding();
  return request<Onboarding>('/onboarding', { signal });
}

/** GET /notifications — hộp thư thông báo của user + số chưa đọc (S19, ADM-F-04). */
export function getNotifications(unreadOnly = false, signal?: AbortSignal): Promise<NotificationList> {
  if (USE_MOCK) return mockGetNotifications(unreadOnly);
  const qs = unreadOnly ? '?unread_only=true' : '';
  return request<NotificationList>(`/notifications${qs}`, { signal });
}

/** POST /notifications/{id}/read — đánh dấu 1 thông báo đã đọc (S19). */
export function markNotificationRead(id: string): Promise<Notification> {
  if (USE_MOCK) return mockMarkNotificationRead(id);
  return request<Notification>(`/notifications/${encodeURIComponent(id)}/read`, { method: 'POST', idempotent: true });
}

/** POST /notifications/read-all — đánh dấu tất cả đã đọc (S19). */
export function markAllNotificationsRead(): Promise<{ updated: number }> {
  if (USE_MOCK) return mockMarkAllNotificationsRead();
  return request<{ updated: number }>('/notifications/read-all', { method: 'POST', idempotent: true });
}

/** GET /subscriptions — tài liệu đang theo dõi (S19, ADM-F-04). */
export function listSubscriptions(signal?: AbortSignal): Promise<Subscription[]> {
  if (USE_MOCK) return mockListSubscriptions();
  return request<{ data: Subscription[]; next_cursor: string | null }>('/subscriptions', { signal }).then((p) => p.data);
}

/** POST /documents/{docId}/subscribe — theo dõi tài liệu (S19). */
export function subscribeDocument(docId: string): Promise<SubscriptionStatus> {
  if (USE_MOCK) return mockSubscribeDocument(docId);
  return request<SubscriptionStatus>(`/documents/${encodeURIComponent(docId)}/subscribe`, { method: 'POST', idempotent: true });
}

/** DELETE /documents/{docId}/subscribe — bỏ theo dõi (S19). */
export function unsubscribeDocument(docId: string): Promise<SubscriptionStatus> {
  if (USE_MOCK) return mockUnsubscribeDocument(docId);
  return request<SubscriptionStatus>(`/documents/${encodeURIComponent(docId)}/subscribe`, { method: 'DELETE', idempotent: true });
}

/** GET /reports/issuance — báo cáo ban hành + sức khoẻ (S15, RPT-F-01/02). RBAC admin.config. */
export function getIssuanceReport(signal?: AbortSignal): Promise<IssuanceReport> {
  if (USE_MOCK) return mockGetIssuanceReport();
  return request<IssuanceReport>('/reports/issuance', { signal });
}

/** GET /reports/compliance — % ack tài liệu bắt buộc + ai chưa đọc (S15, RPT-F-04). RBAC admin.config. */
export function getComplianceReport(signal?: AbortSignal): Promise<ComplianceReport> {
  if (USE_MOCK) return mockGetComplianceReport();
  return request<ComplianceReport>('/reports/compliance', { signal });
}

/** GET /reports/feedback — thống kê feedback (S15, RPT-F-05). RBAC admin.config. */
export function getFeedbackReport(signal?: AbortSignal): Promise<FeedbackReport> {
  if (USE_MOCK) return mockGetFeedbackReport();
  return request<FeedbackReport>('/reports/feedback', { signal });
}

/** GET /reports/access — truy cập proxy theo ack (S15, RPT-F-03). RBAC admin.config. */
export function getAccessReport(signal?: AbortSignal): Promise<AccessReport> {
  if (USE_MOCK) return mockGetAccessReport();
  return request<AccessReport>('/reports/access', { signal });
}

/** GET /documents/{doc_id}/diff — unified diff giữa 2 version (S6, DOC-F-06). */
export function getDiff(docId: string, from: string, to: string, signal?: AbortSignal): Promise<DiffResult> {
  if (USE_MOCK) return mockGetDiff(docId, from, to);
  const qs = new URLSearchParams({ from, to }).toString();
  return request<DiffResult>(`/documents/${encodeURIComponent(docId)}/diff?${qs}`, { signal });
}

/** GET /review-queue — InReview (chờ duyệt S9) hoặc Approved (chờ ban hành S8). RBAC doc.approve. */
export function listReviewQueue(
  status: 'InReview' | 'Approved' = 'InReview',
  signal?: AbortSignal,
): Promise<ReviewQueueItem[]> {
  if (USE_MOCK) return mockListReviewQueue(status);
  return request<Page<ReviewQueueItem>>(`/review-queue?status=${status}`, { signal }).then((p) => p.data);
}

/** POST /versions/{id}/publish — ban hành (S8): set issue/effective/change_summary (BR-04/07). RBAC doc.approve. */
export function publishVersion(versionId: string, req: PublishRequest): Promise<PublishResult> {
  if (USE_MOCK) return mockPublishVersion(versionId, req);
  return request<PublishResult>(`/versions/${encodeURIComponent(versionId)}/publish`, {
    method: 'POST',
    body: req,
    idempotent: true,
  });
}

/** POST /versions/{id}/approve — duyệt/từ chối (decision approve|reject; reject cần comment). */
export function approveVersion(
  versionId: string,
  decision: 'approve' | 'reject',
  comment?: string,
): Promise<DocumentVersion> {
  if (USE_MOCK) return mockApproveVersion(versionId, decision);
  return request<DocumentVersion>(`/versions/${encodeURIComponent(versionId)}/approve`, {
    method: 'POST',
    body: { decision, comment },
    idempotent: true,
  });
}

/** POST /documents — tạo tài liệu mới (Draft v1.0). Editor S7 "new". */
export function createDocument(req: CreateDocumentRequest): Promise<DocumentDetail> {
  if (USE_MOCK) return mockCreateDocument(req);
  return request<DocumentDetail>('/documents', { method: 'POST', body: req, idempotent: true });
}

/** GET /versions/{id}/content — nội dung markdown của bản draft để soạn/sửa (S7). */
export function getVersionContent(versionId: string, signal?: AbortSignal): Promise<VersionContent> {
  if (USE_MOCK) return mockGetVersionContent(versionId);
  return request<VersionContent>(`/versions/${encodeURIComponent(versionId)}/content`, { signal });
}

/** PATCH /versions/{id}/content — lưu nội dung bản draft (S7, chỉ Draft/UnderRevision — BR-03). */
export function updateVersionContent(versionId: string, contentMarkdown: string): Promise<VersionContent> {
  if (USE_MOCK) return mockUpdateVersionContent(versionId, contentMarkdown);
  return request<VersionContent>(`/versions/${encodeURIComponent(versionId)}/content`, {
    method: 'PATCH',
    body: { content_markdown: contentMarkdown },
    idempotent: true,
  });
}

/** POST /versions/{id}/submit — gửi duyệt (Draft → InReview). */
export function submitVersion(versionId: string): Promise<DocumentVersion> {
  if (USE_MOCK) return mockSubmitVersion(versionId);
  return request<DocumentVersion>(`/versions/${encodeURIComponent(versionId)}/submit`, {
    method: 'POST',
    idempotent: true,
  });
}

/** POST /versions/{id}/acknowledge — xác nhận đã đọc (acknowledgeVersion). */
export function acknowledgeVersion(versionId: string): Promise<Acknowledgement> {
  if (USE_MOCK) return mockAcknowledgeVersion(versionId);
  return request<Acknowledgement>(`/versions/${encodeURIComponent(versionId)}/acknowledge`, {
    method: 'POST',
    idempotent: true,
  });
}

// ── Admin / S17 (ADM-F-01 RBAC · ADM-F-02 taxonomy) — RBAC admin.config ──

/** GET /admin/rbac — ma trận phân quyền §10.1 (CHỈ ĐỌC). */
export function getRbac(signal?: AbortSignal): Promise<RbacMatrix> {
  if (USE_MOCK) return mockGetRbac();
  return request<RbacMatrix>('/admin/rbac', { signal });
}

/** GET /admin/users — user (lớp chiếu B0) + role. Sửa role làm ở Keycloak. */
export function listAdminUsers(limit = 100, signal?: AbortSignal): Promise<AdminUser[]> {
  if (USE_MOCK) return mockListAdminUsers();
  return request<Page<AdminUser>>(`/admin/users?limit=${limit}`, { signal }).then((p) => p.data);
}

/** GET /admin/tags — danh sách tag + số tài liệu dùng (ADM-F-02). */
export function listTags(signal?: AbortSignal): Promise<AdminTag[]> {
  if (USE_MOCK) return mockListTags();
  return request<Page<AdminTag>>('/admin/tags', { signal }).then((p) => p.data);
}

/** POST /admin/tags — tạo tag mới. */
export function createTag(slug: string, name: string): Promise<AdminTag> {
  if (USE_MOCK) return mockCreateTag(slug, name);
  return request<AdminTag>('/admin/tags', { method: 'POST', body: { slug, name }, idempotent: true });
}

/** PATCH /admin/tags/{id} — đổi slug/tên. */
export function updateTag(id: string, body: { slug?: string; name?: string }): Promise<AdminTag> {
  if (USE_MOCK) return mockUpdateTag(id, body);
  return request<AdminTag>(`/admin/tags/${encodeURIComponent(id)}`, { method: 'PATCH', body, idempotent: true });
}

/** DELETE /admin/tags/{id} — xoá tag (409 nếu đang dùng). */
export function deleteTag(id: string): Promise<void> {
  if (USE_MOCK) return mockDeleteTag(id);
  return request<void>(`/admin/tags/${encodeURIComponent(id)}`, { method: 'DELETE', idempotent: true });
}

/** GET /admin/doc-types — quản trị loại tài liệu (kể cả inactive). */
export function listDocTypes(signal?: AbortSignal): Promise<AdminDocType[]> {
  if (USE_MOCK) return mockListDocTypes();
  return request<Page<AdminDocType>>('/admin/doc-types', { signal }).then((p) => p.data);
}

/** POST /admin/doc-types — thêm loại tài liệu mới (code ổn định + label). */
export function createDocType(code: string, label?: string, seq?: number): Promise<AdminDocType> {
  if (USE_MOCK) return mockCreateDocType(code, label);
  return request<AdminDocType>('/admin/doc-types', { method: 'POST', body: { code, label, seq }, idempotent: true });
}

/** PATCH /admin/doc-types/{id} — sửa label/seq/active (code bất biến). */
export function updateDocType(id: string, body: { label?: string; seq?: number; active?: boolean }): Promise<AdminDocType> {
  if (USE_MOCK) return mockUpdateDocType(id, body);
  return request<AdminDocType>(`/admin/doc-types/${encodeURIComponent(id)}`, { method: 'PATCH', body, idempotent: true });
}

/** DELETE /admin/doc-types/{id} — xoá loại (409 nếu đang dùng). */
export function deleteDocType(id: string): Promise<void> {
  if (USE_MOCK) return mockDeleteDocType(id);
  return request<void>(`/admin/doc-types/${encodeURIComponent(id)}`, { method: 'DELETE', idempotent: true });
}

/** GET /documents/{docId}/audience — audience roles của tài liệu (ADM-F-02). */
export function getDocAudience(docId: string, signal?: AbortSignal): Promise<DocAudienceItem[]> {
  if (USE_MOCK) return mockGetDocAudience(docId);
  return request<Page<DocAudienceItem>>(`/documents/${encodeURIComponent(docId)}/audience`, { signal }).then((p) => p.data);
}

/** PUT /documents/{docId}/audience — đặt lại toàn bộ audience roles. */
export function setDocAudience(docId: string, audience: DocAudienceItem[]): Promise<DocAudienceItem[]> {
  if (USE_MOCK) return mockSetDocAudience(docId, audience);
  return request<Page<DocAudienceItem>>(`/documents/${encodeURIComponent(docId)}/audience`, {
    method: 'PUT',
    body: { audience },
    idempotent: true,
  }).then((p) => p.data);
}

export const _internal = { newIdempotencyKey };
