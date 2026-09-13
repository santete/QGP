/**
 * Type alias tiện dụng — nguồn duy nhất là schema.d.ts (sinh từ
 * product-spec/api/openapi.yaml qua `npm run gen:api`).
 * KHÔNG khai báo lại shape ở đây — luôn trỏ về components['schemas'].
 */
import type { components } from './schema';

export type VersionStatus = components['schemas']['VersionStatus'];
export type DocumentType = components['schemas']['DocumentType'];
export type DocumentSummary = components['schemas']['DocumentSummary'];
export type Document = components['schemas']['Document'];
export type DocumentDetail = components['schemas']['DocumentDetail'];
export type DocumentVersion = components['schemas']['DocumentVersion'];
export type ReviewQueueItem = components['schemas']['ReviewQueueItem'];
export type PublishResult = components['schemas']['PublishResult'];
export type PublishRequest = components['schemas']['PublishRequest'];
export type SearchHit = components['schemas']['SearchHit'];
export type AuditEntry = components['schemas']['AuditEntry'];
export type Feedback = components['schemas']['Feedback'];
export type FeedbackCategory = components['schemas']['FeedbackCategory'];
export type FeedbackStatus = components['schemas']['FeedbackStatus'];
export type CreateFeedbackRequest = components['schemas']['CreateFeedbackRequest'];
export type VersionContent = components['schemas']['VersionContent'];
export type CreateDocumentRequest = components['schemas']['CreateDocumentRequest'];
export type Recommendations = components['schemas']['Recommendations'];
export type RecommendationItem = components['schemas']['RecommendationItem'];
export type Onboarding = components['schemas']['Onboarding'];
export type OnboardingItem = components['schemas']['OnboardingItem'];
export type Notification = components['schemas']['Notification'];
export type NotificationList = components['schemas']['NotificationList'];
export type Subscription = components['schemas']['Subscription'];
export type SubscriptionStatus = components['schemas']['SubscriptionStatus'];

/** Kết quả diff 2 version (S6, DOC-F-06) — mirror inline response openapi `getDiff`. */
export interface DiffResult {
  from: string;
  to: string;
  diff: string;
  change_summary?: string;
}
export type Acknowledgement = components['schemas']['Acknowledgement'];
export type ApiErrorBody = components['schemas']['Error'];

/** Báo cáo ban hành + sức khoẻ (RPT-F-01/02, S15) — openapi trả object mở, mirror DTO BE. */
export interface IssuanceReport {
  total_documents: number;
  effective_count: number;
  by_type: Record<string, number>;
  by_version_status: Record<string, number>;
  issued_this_month: number;
  overdue_review: number;
  due_soon_review: number;
  with_change_summary_percent: number;
}
export interface ComplianceItem {
  doc_id: string;
  title: string;
  effective_version?: string | null;
  audience_count: number;
  acked_count: number;
  percent: number;
  not_read: string[];
}
export interface ComplianceReport {
  mandatory_docs: number;
  fully_compliant_docs: number;
  items: ComplianceItem[];
}
/** Thống kê feedback (RPT-F-05, S15). */
export interface FeedbackReport {
  total: number;
  by_status: Record<string, number>;
  avg_resolution_hours?: number | null;
  resolved_count: number;
}
export interface AccessItem {
  doc_id: string;
  title: string;
  ack_count: number;
}
/** Truy cập proxy (RPT-F-03, S15) — theo lượt ack; note nêu rõ giới hạn (chưa có event lượt xem). */
export interface AccessReport {
  effective_with_zero_ack: number;
  zero_ack_docs: AccessItem[];
  top_engaged: AccessItem[];
  note: string;
}

// ── Admin / S17 (ADM-F-01 RBAC · ADM-F-02 taxonomy) — openapi loose → local interface (tiền lệ DiffResult) ──
export interface RoleInfo {
  code: string;
  name: string;
}
export interface RbacPolicy {
  policy: string;
  roles: string[];
}
/** Ma trận phân quyền §10.1 (CHỈ ĐỌC — nguồn authz thật là Keycloak). */
export interface RbacMatrix {
  roles: RoleInfo[];
  policies: RbacPolicy[];
}
/** User trong app (lớp chiếu B0) + role. Sửa role làm ở Keycloak. */
export interface AdminUser {
  sub: string;
  display_name?: string | null;
  email?: string | null;
  roles: string[];
}
export interface AdminTag {
  id: string;
  slug: string;
  name: string;
  doc_count: number;
}
/** Loại tài liệu (doc-types) — code dùng ở API/DB, label để hiển thị. */
export interface AdminDocType {
  id: string;
  code: string;
  label: string;
  seq: number;
  active: boolean;
  doc_count: number;
}
/** Audience role của 1 tài liệu (ADM-F-02). */
export interface DocAudienceItem {
  role: string;
  reason?: string | null;
}

/** 7 trạng thái vòng đời version (state machine SDD §4.3). */
export const VERSION_STATUSES: readonly VersionStatus[] = [
  'Draft',
  'InReview',
  'Approved',
  'Published',
  'Effective',
  'Superseded',
  'Retired',
];
