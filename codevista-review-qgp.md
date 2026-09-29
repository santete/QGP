# Báo cáo Review QMS Governance Platform (QGP)

> Ngày review: 2026-09-13
> Reviewer: CodeVista (AI agent)
> Phạm vi: CLAUDE.md, .claude/memory/*, product-spec/, docs/ai/, apps/api/, apps/web/
> Mọi thông tin dẫn nguồn cụ thể (tên file, số dòng). Chỗ chưa xác minh ghi rõ "chưa xác minh".

---

## Mục lục

1. [Sản phẩm: làm gì, cho ai, vai trò, quyền hạn](#1-sản-phẩm)
2. [Kiến trúc và luồng nghiệp vụ chính](#2-kiến-trúc-và-luồng-nghiệp-vụ-chính)
3. [Quy ước code đang áp dụng](#3-quy-ước-code-đang-áp-dụng)
4. [Tiến độ hiện tại: đã xong, còn lại](#4-tiến-độ-hiện-tại)

---

## 1. Sản phẩm

### 1.1 QGP là gì?

**Quality Governance Portal (QGP)** — platform nội bộ FPT ISC, quản trị tài liệu quy trình QA theo nguyên tắc docs-as-code (ADR-0001, `product-spec/adr/0001-docs-as-code-architecture.md`). Nội dung quy trình là Markdown + frontmatter lưu trong Git (immutable per commit, BR-03), QGP render HTML và quản lý metadata/lifecycle/RBAC/feedback/onboarding/recommendation trong PostgreSQL.

> Nguồn: `product-spec/qms-governance-platform-spec.md` §1 (6 nhu cầu → 6 module: DOC, KB, ONB, RPT, FBK, BOT + ADM); `product-spec/SDD_QGP_v1.0.src.md` §4.3 (state machine WF-01); ADR-0001 (docs-as-code).

### 1.2 Cho ai dùng?

Nội bộ FPT ISC — nhân viên QA, QA Lead, Quản trị hệ thống. 6 vai trò RBAC (SDD §6.2 / PRD §10.1):

| Role | Mã | Nguồn định nghĩa |
|------|----|------------------|
| Người đọc | READER | `apps/api/src/Qgp.Api/Auth/QgpAuth.cs:6` |
| Cộng tác viên | CONTRIBUTOR | `apps/api/src/Qgp.Api/Auth/QgpAuth.cs:7` |
| Tác giả | AUTHOR | `apps/api/src/Qgp.Api/Auth/QgpAuth.cs:8` |
| Người duyệt | APPROVER | `apps/api/src/Qgp.Api/Auth/QgpAuth.cs:9` |
| QA Governance Lead | QA_LEAD | `apps/api/src/Qgp.Api/Auth/QgpAuth.cs:10` |
| Quản trị | ADMIN | `apps/api/src/Qgp.Api/Auth/QgpAuth.cs:11` |

> Seed 6 roles: `apps/api/src/Qgp.Api/Infrastructure/Persistence/QgpDbContext.cs` (HasData Guid 1111..1101-1106), `apps/api/src/Qgp.Api/Domain/Entities/Identity.cs`.

### 1.3 Quyền hạn (RBAC §10.1)

5 policy, mỗi policy = tập role được phép (default deny). Nguồn: `apps/api/src/Qgp.Api/Auth/QgpAuth.cs:21-37`:

| Policy | Mã | Role được phép | Nguồn dòng |
|--------|----|---------------|------------|
| Đọc Effective | `doc.read` | Tất cả (READER..ADMIN) | `QgpAuth.cs:32` |
| Đóng góp KB | `kb.contribute` | CONTRIBUTOR, AUTHOR, APPROVER, QA_LEAD, ADMIN | `QgpAuth.cs:33` |
| Soạn/sửa DOC | `doc.author` | AUTHOR, APPROVER, QA_LEAD | `QgpAuth.cs:34` |
| Duyệt DOC | `doc.approve` | APPROVER, QA_LEAD | `QgpAuth.cs:35` |
| Cấu hình/audit | `admin.config` | QA_LEAD, ADMIN | `QgpAuth.cs:36` |

> FE mirror role group: `apps/web/src/auth/roles.ts:33` (ADMIN_ROLES = [QA_LEAD, ADMIN]), `:36` (APPROVE_ROLES = [APPROVER, QA_LEAD]), `:39` (AUTHOR_ROLES = [AUTHOR, APPROVER, QA_LEAD]).

---

## 2. Kiến trúc và luồng nghiệp vụ chính

### 2.1 Tổng quan kiến trúc

```
Keycloak (OIDC IdP) → JWT (realm_access.roles → KeycloakClaimsTransformation)
    ↓
Frontend (apps/web) React 18 + Vite + TS
    ↓ HTTP /v1 (JSON snake_case, ADR-0014 envelope)
Backend (apps/api) .NET 8 minimal API
    ├── Domain (Entities/Enums)
    ├── Application (19 services)
    ├── Infrastructure (Persistence EF Core, Search Meilisearch, Scheduling Quartz, Git LibGit2Sharp)
    └── Auth (JwtBearer, ClaimsTransformation, DevTokenIssuer)
    ↓
PostgreSQL 16 (schema "qgp", 16+ bảng, snake_case)
Meilisearch (index "documents", chỉ Effective BR-06)
Git bare repo (content_git_ref = SHA khi publish, ADR-0015)
```

> Nguồn: `apps/api/src/Qgp.Api/Program.cs` (DI + middleware), `apps/api/src/Qgp.Api/Api/V1Endpoints.cs` (237 dòng, tất cả endpoint /v1), `apps/api/src/Qgp.Api/Auth/AuthSetup.cs` (104 dòng, JWT + RBAC setup).

### 2.2 Backend (apps/api) — .NET 8

**Entry point:** `apps/api/src/Qgp.Api/Program.cs` — DI: QgpDbContext, 19 scoped services, Quartz (3 jobs: EffectiveTransition, AuditPartitionMaintenance, ReviewReminder), JwtBearer (Dev/Oidc), RateLimiter, AddKeyPerFile, ISC.Observability, CORS, ForwardedHeaders, security headers.

**Lớp Application (19 tập tin, `apps/api/src/Qgp.Api/Application/`):**

| File | Dòng | Trách nhiệm | Nguồn xác minh |
|------|------|-------------|----------------|
| `AdminService.cs` | — | RBAC read, users list, tags CRUD, doc-types CRUD | `list_files` |
| `AppException.cs` | — | Custom exception | `list_files` |
| `AuditPartitionService.cs` | 38 | Tạo partition audit_logs tháng kế (raw DDL, idempotent, SDD §2.4) | `subagent` read |
| `AuditService.cs` | — | Audit logging (append-only) | `list_files` |
| `DocumentService.cs` | — | Document CRUD, list, audience, diff (DOC-F-06) | `V1Endpoints.cs:16-36,188-194` |
| `EffectiveTransition.cs` | 32 | Static helper: chuyển DocumentVersion→Effective + supersede (BR-02) | `subagent` read |
| `EffectiveTransitionService.cs` | — | Scheduler Published→Effective (gọi EffectiveTransition.Apply) | `project_state.yaml` completed_tasks |
| `FeedbackService.cs` | 91 | In-context feedback: create/list/triage (FBK-F-03/04) | `subagent` read, `V1Endpoints.cs:56-69` |
| `Mapping.cs` | — | DTO mapping | `list_files` |
| `NotificationService.cs` | — | Notification creation for audience ∪ subscribers | `project_state.yaml` |
| `OnboardingService.cs` | — | Learning path + checklist % | `V1Endpoints.cs:77-79` |
| `RecommendationService.cs` | — | Rules-based explainable REC (BR-11) | `V1Endpoints.cs:72-74` |
| `ReportService.cs` | — | Issuance, compliance, feedback, access | `V1Endpoints.cs:82-96` |
| `ReviewReminderService.cs` | — | DOC-F-08 review reminders (Quartz daily) | `project_state.yaml` |
| `SearchService.cs` | — | Meilisearch wrapper (index Effective only BR-06) | `V1Endpoints.cs:21-23` |
| `SubscriptionService.cs` | — | Doc subscriptions (S19) | `V1Endpoints.cs:114-124` |
| `UnifiedDiff.cs` | 66 | Line-based unified diff (LCS DP, DOC-F-06) | `subagent` read |
| `UserProvisioningService.cs` | — | /me sync user + roles from Keycloak (B0 projection) | `project_state.yaml` |
| `VersionService.cs` | — | Version lifecycle: create, content, submit, approve, publish, acknowledge | `V1Endpoints.cs:197-227` |

**Endpoint /v1 (V1Endpoints.cs, 237 dòng):** 35+ endpoint, RBAC theo từng route:

| Endpoint | Dòng | RBAC | Mô tả |
|----------|------|------|-------|
| GET /v1/documents | 16-18 | doc.read | List documents (q, type, tag, limit) |
| GET /v1/search | 21-23 | doc.read | Full-text search (Meilisearch, chỉ Effective BR-06) |
| GET /v1/documents/{docId} | 25-27 | doc.read | Document detail |
| GET /v1/documents/{docId}/versions | 29-31 | doc.read | Version list |
| GET /v1/documents/{docId}/diff | 34-36 | doc.read | Diff 2 version (S6, DOC-F-06) |
| POST /v1/documents | 38-43 | doc.author | Tạo document |
| GET /v1/review-queue | 46-53 | doc.approve | Hàng đợi duyệt (InReview/Approved) |
| POST /v1/feedback | 56-58 | doc.read | Gửi feedback (S13) |
| GET /v1/feedback | 60-65 | admin.config | List feedback (S14 triage) |
| PATCH /v1/feedback/{id} | 67-69 | admin.config | Triage feedback (đổi status) |
| GET /v1/recommendations | 72-74 | doc.read | Gợi ý REC (S2/S11, BR-11) |
| GET /v1/onboarding | 77-79 | doc.read | Learning path (S12) |
| GET /v1/reports/issuance | 82-84 | admin.config | Báo cáo issuance (S15) |
| GET /v1/reports/compliance | 86-88 | admin.config | Báo cáo compliance |
| GET /v1/reports/feedback | 90-92 | admin.config | Báo cáo feedback |
| GET /v1/reports/access | 94-96 | admin.config | Báo cáo access (proxy theo ack) |
| GET /v1/notifications | 99-103 | doc.read | Notification list (S19, bell) |
| POST /v1/notifications/{id}/read | 105-107 | doc.read | Mark notification read |
| POST /v1/notifications/read-all | 109-111 | doc.read | Mark all read |
| GET /v1/subscriptions | 114-116 | doc.read | List subscriptions (S19) |
| POST /v1/documents/{docId}/subscribe | 118-120 | doc.read | Subscribe |
| DELETE /v1/documents/{docId}/subscribe | 122-124 | doc.read | Unsubscribe |
| GET /v1/audit | 127-132 | admin.config | Audit trail (S16, ADM-F-03) |
| GET /v1/admin/rbac | 135-137 | admin.config | RBAC matrix read-only (ADM-F-01) |
| GET /v1/admin/users | 139-141 | admin.config | Users list (projection B0) |
| GET /v1/admin/tags | 143-145 | admin.config | List tags |
| POST /v1/admin/tags | 147-149 | admin.config | Create tag |
| PATCH /v1/admin/tags/{id} | 151-153 | admin.config | Update tag |
| DELETE /v1/admin/tags/{id} | 155-160 | admin.config | Delete tag (chặn TAG_IN_USE) |
| GET /v1/doc-types | 163-165 | doc.read | List active doc-types (form/filter) |
| GET /v1/admin/doc-types | 168-170 | admin.config | List all doc-types (kể cả inactive) |
| POST /v1/admin/doc-types | 172-174 | admin.config | Create doc-type |
| PATCH /v1/admin/doc-types/{id} | 176-178 | admin.config | Update doc-type (code bất biến) |
| DELETE /v1/admin/doc-types/{id} | 180-185 | admin.config | Delete doc-type (chặn DOC_TYPE_IN_USE) |
| GET /v1/documents/{docId}/audience | 188-190 | admin.config | Get audience roles |
| PUT /v1/documents/{docId}/audience | 192-194 | admin.config | Set audience roles (replace all) |
| POST /v1/documents/{docId}/versions | 197-202 | doc.author | Tạo version mới |
| GET /v1/versions/{id}/content | 205-207 | doc.author | Đọc content markdown (S7 editor) |
| PATCH /v1/versions/{id}/content | 209-211 | doc.author | Sửa content markdown (S7 editor) |
| POST /v1/versions/{id}/submit | 213-215 | doc.author | Submit for review (InReview) |
| POST /v1/versions/{id}/approve | 217-219 | doc.approve | Approve (Approved) |
| POST /v1/versions/{id}/publish | 221-223 | doc.approve | Publish (Published→Effective + supersede BR-02) |
| POST /v1/versions/{id}/acknowledge | 225-227 | doc.read | Acknowledge (per-user, BR-05/08) |

> Helper: `Sub(u)` = claim "sub" (UUID, `V1Endpoints.cs:232`), `Roles(u)` = claim "role" (`V1Endpoints.cs:235-236`).

**Auth (`apps/api/src/Qgp.Api/Auth/`):**

| File | Dòng | Trách nhiệm |
|------|------|-------------|
| `AuthSetup.cs` | 1-104 | JWT Bearer (Dev=HS256 / Oidc=RS256), AddQgpPolicies từ QgpPolicies.RolesFor (`:98-103`), RequireHttpsMetadata configurable (`:30`) |
| `AuthEndpoints.cs` | — | /auth/dev-login (Dev only), /me (UserProvisioningService sync), /admin/reindex (AdminConfig) |
| `QgpAuth.cs` | 1-38 | QgpRoles (6 roles) + QgpPolicies (5 policies, RolesFor mapping) |
| `KeycloakClaimsTransformation.cs` | — | Flatten realm_access.roles → ClaimTypes.Role qua Auth:RoleMap |

**Auth modes** (`AuthSetup.cs:19-63`):
- **Dev** (`Auth:Mode=Dev`): HS256 mock, DevTokenIssuer, key từ `QGP_AUTH_SIGNING_KEY` env hoặc ephemeral (`:81-96`)
- **Oidc** (`Auth:Mode=Oidc`): Keycloak RS256/JWKS, authority từ `Auth:OidcAuthority`, KeycloakClaimsTransformation (`:25-46`)

**State machine WF-01** (`apps/api/src/Qgp.Api/Domain/Enums/VersionStatus.cs:8-18`):
```
Draft → InReview → Approved → Published → Effective
                ↘ (reject) → Draft
Published + effective_date ≤ now → Effective + supersede bản cũ (BR-02)
```
8 trạng thái: 7 public (Draft, InReview, Approved, Published, Effective, Superseded, Retired) + UnderRevision (nội bộ, `VersionStatus.cs:17`).

**Scheduler (3 Quartz jobs):**
1. `EffectiveTransitionJob` — Published→Effective + supersede (WF-03, idempotent)
2. `AuditPartitionMaintenanceJob` — tạo partition audit_logs tháng kế (SDD §2.4)
3. `ReviewReminderJob` — quét next_review_date ≤30d + overdue → notify QA_LEAD/ADMIN (DOC-F-08)

> Nguồn: `apps/api/src/Qgp.Api/Infrastructure/Scheduling/` (3 files), `project_state.yaml` completed_tasks.

**Database (PostgreSQL 16, schema "qgp", 16+ bảng):**
- Đầy đủ schema: `.claude/memory/schema_snapshot.yaml` (database section)
- 8 migrations: `apps/api/src/Qgp.Api/Infrastructure/Persistence/Migrations/` (InitialCreate → AddDocTypes)
- Bảng chính: documents, document_versions, roles, users, user_roles, tags, doc_types, doc_tags, doc_audience_roles, doc_relations, acknowledgements, feedback, audit_logs (partitioned), learning_paths, path_items, notifications, subscriptions
- Enum lưu text (HasConversion<string>), ID = UUID gen_random_uuid()

### 2.3 Frontend (apps/web) — React 18 + Vite + TS

**16 pages** (`apps/web/src/pages/`): HomePage, StartHerePage, OnboardingPage, DocumentsPage, KbPage, DocumentPage, HistoryPage, EditorPage, ReviewQueuePage, PublishPage, AdminPage, AuditPage, FeedbackTriagePage, ReportsPage, LoginPage, ForbiddenPage.

**Routing** (`apps/web/src/router.tsx`, 74 dòng — đầy đủ với số dòng):

| Route | Dòng | Page | RBAC |
|-------|------|------|------|
| `/login` | 37 | LoginPage | public |
| `/403` | 38 | ForbiddenPage | public |
| `/` | 42 | HomePage | RequireAuth (doc.read) |
| `/start` | 43 | StartHerePage | RequireAuth (doc.read) |
| `/onboarding` | 44 | OnboardingPage | RequireAuth (doc.read) |
| `/documents` | 45 | DocumentsPage | RequireAuth (doc.read) |
| `/kb` | 46 | KbPage | RequireAuth (doc.read) — S10 KB-F-03, browse Effective nhóm theo loại (112 dòng) |
| `/documents/:docId` | 47 | DocumentPage | RequireAuth (doc.read) — S4 view + acknowledge |
| `/documents/:docId/history` | 48 | HistoryPage | RequireAuth (doc.read) — xem lịch sử version + unified diff (136 dòng) |
| `/editor/new` | 52 | EditorPage | RequireAuth + AUTHOR_ROLES (AUTHOR, APPROVER, QA_LEAD) — tạo doc mới (233 dòng) |
| `/editor/:versionId` | 53 | EditorPage | RequireAuth + AUTHOR_ROLES — soạn thảo draft |
| `/review` | 59 | ReviewQueuePage | RequireAuth + APPROVE_ROLES (APPROVER, QA_LEAD) — duyệt InReview (149 dòng) |
| `/publish` | 60 | PublishPage | RequireAuth + APPROVE_ROLES — publish Approved→Effective (170 dòng) |
| `/admin` | 66 | AdminPage (4 tabs) | RequireAuth + ADMIN_ROLES (QA_LEAD, ADMIN) — RBAC/Users/Tags/DocTypes |
| `/audit` | 67 | AuditPage | RequireAuth + ADMIN_ROLES — audit log table S16 ADM-F-03 (90 dòng) |
| `/feedback` | 68 | FeedbackTriagePage | RequireAuth + ADMIN_ROLES — S14 FBK-F-03, list + triage feedback (115 dòng) |
| `/reports` | 69 | ReportsPage | RequireAuth + ADMIN_ROLES — S15 dashboard |

> ProtectedLayout (dòng 22-30) bọc RequireAuth + AppShell cho mọi route con.
> Role groups: AUTHOR_ROLES (`roles.ts:39`), APPROVE_ROLES (`roles.ts:36`), ADMIN_ROLES (`roles.ts:33`).

**3 FE modes** (`apps/web/package.json` scripts):
- `dev` (mock): VITE_USE_MOCK=1, không cần BE
- `dev:real`: gọi BE /auth/dev-login (Dev mode)
- `dev:oidc`: Keycloak thật (OIDC redirect, PKCE, `apps/web/.env.oidc`)

### 2.4 Luồng nghiệp vụ chính

1. **Auth:** Keycloak → JWT (realm_access.roles) → `KeycloakClaimsTransformation.cs` flatten + map RoleMap → ASP.NET policies (5 policy, `QgpAuth.cs:21-37`) → /me (`UserProvisioningService.SyncAsync` upsert user + reconcile roles B0)
2. **Document lifecycle (WF-01):** Draft→InReview→Approved→Published→Effective (`VersionStatus.cs:8-18`). Publish → commit Git (content_git_ref = SHA, ADR-0015) + index Meilisearch (Effective only, BR-06) + notify audience ∪ subscribers. Scheduler auto Published→Effective (Quartz, `EffectiveTransitionJob`).
3. **Search (E1.4):** Meilisearch index "documents" (primaryKey=doc_id), searchable [title,content,tags], filterable [type,tags]. CHỈ index Effective (BR-06). HttpClient thô (SDK 401 workaround, `schema_snapshot.yaml` gotcha).
4. **Recommendation (E2.1):** rules-based explainable (BR-11, SDD §5.2), reason tiếng Việt ("Bắt buộc"/"Cần đọc lại"/"Vừa cập nhật"/"Khớp vai trò"), read-only, DEFER Redis cache.
5. **Onboarding (E2.2):** learning_paths per role, checklist %, ack = bản Effective hiện hành.
6. **Reports (E2.3):** 4 báo cáo — issuance (RPT-F-01/02), compliance (audience ∩ user_roles), feedback (RPT-F-05), access (RPT-F-03, proxy theo ack).
7. **Notifications (E2.4):** bell dropdown (`AppShell.tsx`), sinh khi doc Effective cho audience ∪ subscribers, type: doc_effective / doc_review_due / doc_review_overdue.
8. **Admin (B1/S17):** RBAC read-only (ma trận §10.1), users (projection B0), tags CRUD (slug unique), doc-types CRUD (code bất biến), audience (PUT replace all).
9. **Feedback (S13/S14, FBK-F-03/04):** `FeedbackService.cs` (91 dòng) create (doc.read) + list/triage (admin.config). FE `FeedbackTriagePage.tsx` (115 dòng, `router.tsx:68`).
10. **KB (S10, KB-F-03):** `KbPage.tsx` (112 dòng, `router.tsx:46`) — browse Effective nhóm theo loại, filter client-side.
11. **Editor (S7):** `EditorPage.tsx` (233 dòng, `router.tsx:52-53`) — dual-mode NewDocumentForm + EditDraft, PATCH content + submit for review.
12. **Review queue (S9):** `ReviewQueuePage.tsx` (149 dòng, `router.tsx:59`) — list InReview, approve/reject.
13. **Publish (S8):** `PublishPage.tsx` (170 dòng, `router.tsx:60`) — list Approved, publish form (issue date, effective date, change summary).
14. **History (S6, DOC-F-06):** `HistoryPage.tsx` (136 dòng, `router.tsx:48`) — unified diff 2 version (UnifiedDiff.cs LCS DP).
15. **Audit (S16, ADM-F-03):** `AuditPage.tsx` (90 dòng, `router.tsx:67`) — audit log table.

---

## 3. Quy ước code đang áp dụng

### 3.1 Pipeline (CLAUDE.md)

6-phase: Phase 0 (Context Load) → Phase 1 (Plan, 6D classification) → Phase 2 (Implement, TDD-first) → Phase 3 (Verify) → Phase 4 (Self-Review) → Phase 5 (Report). Hard stops: schema migration, secrets, breaking API, production env. WISC gates: WRITE/ISOLATE/SELECT/COMPRESS.

> Nguồn: `CLAUDE.md` (385 dòng, toàn file).

### 3.2 Coding rules (docs/ai/)

| File | Quy tắc chính | Nguồn |
|------|---------------|------|
| `docs/ai/CODING_RULES.md` | kebab-case files, PascalCase classes, camelCase vars, ~300 dòng/file, no `any`/`@ts-ignore`, no empty catch | `subagent` read |
| `docs/ai/API_RULES.md` | OpenAPI 3.1 single source, snake_case JSON, /v1 prefix, validate at boundary | `subagent` read |
| `docs/ai/DB_RULES.md` | snake_case, schema qgp (không public), no sửa migration cũ, enum text | `subagent` read |
| `docs/ai/SECURITY_RULES.md` | Keycloak OIDC, 5 RBAC policies, file-secrets, no hardcoded secrets | `subagent` read |
| `docs/ai/TESTING_RULES.md` | TDD-first, WebApplicationFactory (Testing env), Vitest + Testing Library | `subagent` read |
| `docs/ai/GIT_CONVENTION.md` | Conventional Commits `type(scope): desc [AI]`, 8-section MR | `subagent` read |
| `docs/ai/HALLUCINATION_RULES.md` | Cite source (file:line), schema_snapshot là single source, nullable handling, challenge assumptions, 5 loại hallucination | `CLAUDE.md:38` (eager load) |

### 3.3 Internal rules (docs/ai/internal_rules/)

| File | Quy tắc | Nguồn |
|------|---------|------|
| `00_INDEX.md` | Decision Tree (A/B/C/D/E), BLOCKER table, cross-cutting refs | `CLAUDE.md:28` (eager load nếu thư mục tồn tại) |
| `01_MR_Compliance.md` | R-BRANCH, R-COMMIT (+[AI] tag), R-MR-002 (8 sections), R-MR-003-AI-DISCLOSURE | `subagent` read |
| `02_Naming_Microservice.md` | SQL/Mongo/API path/event/branch naming, R-DECISION | `CLAUDE.md:31` |
| `03_API_Naming.md` | REST endpoint, path, HTTP method, query, OpenAPI | `subagent` read |
| `04_API_Response_and_Error.md` | Response wrapper 4 field + error category (whitelist ADR-0014 lean envelope cho qgp-api) | `subagent` read |
| `05_API_Timeout.md` | HTTP/gRPC/DB/cache client + retry + cancellation | `CLAUDE.md:33` |
| `06_Coding_Convention.md` | Code change conventions (ưu tiên .NET) | `CLAUDE.md:29` |

### 3.4 Quy ước kiến trúc cụ thể

| Quy ước | Chi tiết | Nguồn |
|---------|----------|-------|
| OpenAPI 3.1 | Không `nullable` (dùng `type: [string, 'null']`), mỗi operation cần 4xx. FE types sinh từ `npm run gen:api` → `src/api/schema.d.ts` | `project_state.yaml` gotcha #1, ADR-0014 |
| Response envelope | Lean `{error:{code,message}}` code UPPER_SNAKE (ADR-0014, whitelist R-RESP-STRUCTURE-001) | `product-spec/adr/0014-api-response-envelope.md` |
| Enum lưu text | HasConversion<string>, 8 trạng thái (7 public + UnderRevision) | `VersionStatus.cs:8-18`, `project_state.yaml` decision #8 |
| schema qgp | UseSnakeCaseNamingConvention + schema "qgp" (không public) | `project_state.yaml` decision #7 |
| Auth 2 chế độ | Dev=HS256 mock, Oidc=Keycloak RS256/JWKS. Role map trong `appsettings.json Auth:RoleMap` | `AuthSetup.cs:19-63` |
| Git content store | Bare repo local + LibGit2Sharp, COMMIT-ON-PUBLISH (ADR-0015) | `product-spec/adr/0015-git-content-store.md` |
| Meilisearch | HttpClient thô (SDK 401 workaround), index chỉ Effective (BR-06) | `schema_snapshot.yaml` services.meilisearch |
| Test BE | WebApplicationFactory, Testing env, DbContext.Model metadata (offline), serialize suite (xunit.runner.json) | `project_state.yaml` decision #9, gotcha #8 |
| FE 3 modes | dev (mock), dev:real, dev:oidc (VITE_OIDC_AUTHORITY) | `apps/web/package.json` scripts |
| TS types từ OpenAPI | openapi-typescript → schema.d.ts, không khai báo tay | `project_state.yaml` decision #5 |

### 3.5 ADRs (15 quyết định kiến trúc)

| ADR | File | Quyết định |
|-----|------|-----------|
| 0001 | `product-spec/adr/0001-docs-as-code-architecture.md` | Markdown + Git (immutable), dynamic web app |
| 0002 | `0002-platform-name-qgp.md` | Quality Governance Portal |
| 0003 | `0003-aggregate-access-metrics.md` | Proxy theo ack, event tracking P3 |
| 0004 | `0004-bot-corpus-effective-only.md` | RAG chỉ index Effective (BR-06) |
| 0005 | `0005-reset-ack-on-major.md` | BR-05/08, ack per-version |
| 0006 | `0006-backend-dotnet.md` | ASP.NET Core 8, EF Core, minimal API |
| 0007 | `0007-rag-python-service.md` | Phase 3, Python service riêng |
| 0008 | `0008-postgres-pgvector.md` | Postgres 16, schema qgp, snake_case |
| 0009 | `0009-meilisearch-search.md` | Full-text VN, index Effective only |
| 0010 | `0010-frontend-react.md` | React 18 + Vite + TS, Tailwind |
| 0011 | `0011-gitlab-ci-docker.md` | Multi-stage build, non-root, compose prod |
| 0012 | `0012-vault-secrets.md` | File-secrets /run/secrets, AddKeyPerFile |
| 0013 | `0013-otel-observability.md` | OTLP collector + Grafana stack |
| 0014 | `0014-api-response-envelope.md` | Lean (error:{code,message}), UPPER_SNAKE |
| 0015 | `0015-git-content-store.md` | Bare repo local + LibGit2Sharp, commit-on-publish |

---

## 4. Tiến độ hiện tại

### 4.1 Đã xong (theo task_tracker.yaml + project_state.yaml completed_tasks)

**Epic A (Deployment go-live):** ✅ TOÀN BỘ (2026-07-26)
- A1a BE OIDC Keycloak (`KeycloakClaimsTransformation.cs`, `AuthSetup.cs`, `AuthEndpoints.cs`, `realm-qgp.json`)
- A1b FE OIDC redirect (react-oidc-context + oidc-client-ts, `AuthContext.tsx` tách Local/Oidc)
- A3+A4 Docker + docker-compose.prod + GitLab CI + CORS
- A5+A6 Security (rate limit, security headers, file-secrets, backup scripts)
- A7-OTel ISC.Observability 1.2.2 (traces/metrics/logs OTLP, gate Otel:OtlpEndpoint)

**Epic B (Governance gaps):** ✅ TOÀN BỘ (2026-07-26)
- B0 User/role projection (`UserProvisioningService.cs`, sync ở /me)
- B2 Review-cycle reminders (`ReviewReminderService.cs` + `ReviewReminderJob.cs`, Quartz daily)
- B1 Admin/S17 (AdminService RBAC read + users + tags CRUD + doc audience + doc-types CHIỀU SÂU [Type enum→string] + AdminPage FE 4 tab)

**Sprint 1-3 (E0-E2):**
- E0.1 docker-compose local + migration (Postgres16+pgvector+redis+meili)
- T5 BE data layer + EF migration + seed 6 roles
- T6 auth (mock OIDC) + RBAC §10.1 + FE login/guard
- E1.1 wire /v1 (DOC lifecycle WF-01 + đọc) + services + RBAC + FE
- E1.2 Scheduler WF-03 (Quartz auto Published→Effective)
- E1.4 Search Meilisearch (chỉ Effective BR-06) + list documents + FE màn Tài liệu S3
- E2.1 REC engine (rules-based explainable BR-11) + Home S2 + StartHere S11
- E2.2 Onboarding S12 (learning path + checklist %)
- E2.3 Reports S15 (issuance + compliance + feedback + access)
- E2.4 Notifications S19 (bell + sinh khi doc Effective)
- FE Phase 2 reconcile DESIGN_BRIEF
- Git content store E1.6 (LibGit2Sharp, commit-on-publish, ADR-0015)

**FE pages đã build (router.tsx):**
- KbPage.tsx (S10 KB-F-03, `router.tsx:46`, 112 dòng) — browse Effective nhóm theo loại
- FeedbackTriagePage.tsx (S14 FBK-F-03, `router.tsx:68`, 115 dòng) — list + triage feedback (FeedbackService.cs BE)
- HistoryPage.tsx (S6 DOC-F-06, `router.tsx:48`, 136 dòng) — unified diff 2 version
- EditorPage.tsx (S7, `router.tsx:52-53`, 233 dòng) — soạn thảo/tạo doc, dual-mode
- ReviewQueuePage.tsx (S9, `router.tsx:59`, 149 dòng) — duyệt InReview, approve/reject
- PublishPage.tsx (S8 DOC-F-03/UC-03, `router.tsx:60`, 170 dòng) — publish Approved→Effective
- AuditPage.tsx (S16 ADM-F-03, `router.tsx:67`, 90 dòng) — audit log table
- AdminPage.tsx (S17, `router.tsx:66`) — 4 tabs RBAC/Users/Tags/DocTypes
- ReportsPage.tsx (S15, `router.tsx:69`) — reports dashboard
- HomePage, StartHerePage, OnboardingPage, DocumentsPage, DocumentPage, LoginPage, ForbiddenPage

**BE services đã build (Application/):** 19 tập tin (AdminService, AppException, AuditPartitionService, AuditService, DocumentService, EffectiveTransition, EffectiveTransitionService, FeedbackService, Mapping, NotificationService, OnboardingService, RecommendationService, ReportService, ReviewReminderService, SearchService, SubscriptionService, UnifiedDiff, UserProvisioningService, VersionService).

**Test:** BE 67/67 passing (xunit + WebApplicationFactory), FE 43/43 passing (Vitest + Testing Library).

### 4.2 Còn lại (theo project_state.yaml + task_tracker.yaml + ROADMAP_NEXT_v1.0.md)

| Priority | Task | Trạng thái | Nguồn |
|----------|------|-----------|-------|
| Low | b1c1 — màn sửa audience per-document | **open** (BE + test xong, chỉ thiếu UI ở DocumentPage) | `task_tracker.yaml:117` |
| Medium | Ops go-live | CSP nginx, ef migrate deploy, Keycloak hardening prod, Vault thật, deploy Grafana/collector | `project_state.yaml:63` (current_sprint) |
| Phase 3 | RAG/BOT S18 | AI chatbot (RAG), value-add, không chặn go-live | `ROADMAP_NEXT_v1.0.md` |
| — | Redis cache cho RecommendationService | Chưa wire code (SDD §5.2 cache 5m, DEFER) | `project_state.yaml` decision #13 |
| — | Access report (RPT-F-03) event tracking thật | Hiện là proxy theo ack, chưa có event lượt xem | `schema_snapshot.yaml` internal_apis.reports.gotcha |

### 4.3 Gotchas quan trọng (25, từ project_state.yaml + schema_snapshot.yaml)

1. **OpenAPI 3.1:** Không `nullable`, mỗi operation cần 4xx. Verify: `npx @redocly/cli lint`.
2. **Document.Type là string** (bảng doc_types, không còn enum DocumentType). Code bất biến, 'Work Instruction' có dấu cách.
3. **DocumentDetail không chứa version UUID** nhưng acknowledgeVersion(id) cần UUID → FE gọi listVersions() song song.
4. **Keycloak realm_access.roles** là JSON array, không phải claim 'role' phẳng → KeycloakClaimsTransformation.
5. **ValidIssuer = authority** (URL realm), không hardcode 'qgp-dev'.
6. **sub = UUID** (không phải username; username ở preferred_username).
7. **aud = 'qgp-api'** cần audience mapper trên client qgp-web.
8. **Meili .NET SDK 0.15** auth GET /tasks 401 → dùng HttpClient thô.
9. **BE test offline** DbContext.Model metadata, serialize suite (xunit.runner.json parallelizeTestCollections=false).
10. **BE image Production** không auto-migrate (DEPLOY.md).
11. **ISC.Observability** gate theo Otel:OtlpEndpoint (off khi rỗng + off Testing).
12. **Rate limit off** mặc định Testing (env leak fix), prod/dev on.
13. **ForwardedHeaders Clear KnownProxies** chỉ an toàn khi ingress duy nhất nginx.

> Đầy đủ 25 gotchas: `.claude/memory/project_state.yaml` (known_gotchas section) + `.claude/memory/schema_snapshot.yaml`.

### 4.4 Đề xuất cải thiện

1. **PROJECT_MAP.md chưa fill** — vẫn template TODO (`docs/ai/PROJECT_MAP.md` toàn file). Nên cập nhật stack thật, folder structure, build/test commands.
2. **b1c1 (audience UI)** — task open duy nhất (`task_tracker.yaml:117`), BE + test xong, chỉ thiếu UI ở DocumentPage.
3. **Redis cache** chưa wire cho RecommendationService (SDD §5.2 cache 5m, DEFER).
4. **Access report (RPT-F-03)** là PROXY theo ack, chưa có event lượt xem thật.
5. **Keycloak prod hardening** chưa hoàn tất (realm-qgp.json chỉ DEV).
6. **Vault thật** chưa deploy (hiện file-secrets /run/secrets, ADR-0012).
7. **OTel collector + Grafana** chưa deploy (ADR-0013, ISC.Observability đã wire).
8. **CSP nginx** chưa configure (chỉ có security headers cơ bản).
9. **pattern ở project_state.yaml vẫn = A** (solo ≤10k LOC) — nên `/classify` lại.
10. **loc_at_classification = 0** — chưa đo thực tế.
11. **DocumentDetail thiếu effective_version_id** → FE phải gọi listVersions() song song (nên bổ sung giảm round-trip).

---

*Review hoàn thành. Tất cả thông tin dẫn nguồn cụ thể (tên file, số dòng) hoặc ghi rõ "chưa xác minh". Nguồn tham khảo: CLAUDE.md, .claude/memory/*, product-spec/ (PRD, SDD v1.0, BUILD_PLAN, ROADMAP, ADRs), docs/ai/* (rules + internal_rules), apps/api/ (V1Endpoints.cs, QgpAuth.cs, AuthSetup.cs, Program.cs, Application/, Domain/, Infrastructure/), apps/web/ (router.tsx, roles.ts, pages/, auth/).*
