---
title: "QGP — Worklog 2026-07-25 (Build Task 1→5)"
date: 2026-07-25
project: Quality Governance Portal (QGP)
phase: "Phase 1 — MVP"
type: worklog
status: done
tags:
  - project/qgp
  - worklog
  - build
  - dotnet
  - react
aliases:
  - QGP Worklog 25/07
  - Build 1-5 QGP
related:
  - "[[QGP PRD v1.0]]"
  - "[[QGP SDD v1.0]]"
  - "[[QGP BUILD_PLAN]]"
---

# QGP — Worklog 2026-07-25

> [!abstract] TL;DR
> Chạy tuần tự **5 đầu việc** trong NEXT-list build của QGP (docs-as-code, quản trị tài liệu QA).
> Hoàn tất: **FE real-mode E2E · Review queue S9 · Search S3 · Editor S7 · Audit actor_id + partition**.
> Kết quả: **BE 24/24 test · FE 19/19 test · typecheck sạch · OpenAPI Redocly valid · mọi task E2E thật** (browser→Vite:5173→BE:5048→Postgres).

**Stack**: React+Vite+TS (Minimal UI, Tailwind) · .NET 8 ASP.NET Core · PostgreSQL 16 (+pgvector) · Meilisearch · Redis · Quartz.NET.

---

## 1. Các đầu việc đã làm

### ✅ Task 1 — FE real-mode E2E (bỏ mock, nối BE thật)
- Proxy Vite **configurable** qua `VITE_API_PROXY_TARGET` (mặc định `:5048`).
- Thêm script `npm run dev:real` + `.env.real` (`VITE_USE_MOCK=0`) — giữ `npm run dev` = mock (offline default, non-breaking).
- Verify: `POST /auth/dev-login` + `GET /v1/documents/QA-PROC-005` xuyên proxy → dữ liệu thật từ Postgres.
- **Files**: `apps/web/vite.config.ts`, `apps/web/.env.real`, `apps/web/package.json`.

### ✅ Task 2 — E1.7 Review Queue (S9)
- **BE**: `GET /v1/review-queue` `[DocApprove]` + schema `ReviewQueueItem` + `DocumentService.ListReviewQueueAsync` (lọc `InReview`, FIFO theo `updated_at`).
- **FE**: `ReviewQueuePage` (duyệt / từ chối — reject bắt buộc comment) + hook `useReviewQueue` + `APPROVE_ROLES` + route `/review` (guard) + link AppShell.
- approve/reject **dùng lại** `POST /versions/{id}/approve` (field `decision`).
- **Test**: BE +2 (submit→queue, READER→403) · FE +3.

### ✅ Task 3 — S3 dùng `/v1/search` (thay `listDocuments`)
- `DocumentsPage` chuyển sang full-text Meilisearch (`searchDocuments`) → hiển thị **snippet**, dùng **score** để rank.
- Thêm `useDocumentSearch`, **xoá** `useDocumentList` (dead code).
- ⚠️ S3 giờ **chỉ hiển thị tài liệu Effective** (search index chỉ Effective — BR-06).

### ✅ Task 4 — E1.6 Editor (S7) — *Git store DEFER*
- **BE**: `GET`/`PATCH /v1/versions/{id}/content` `[DocAuthor]` — chỉ sửa `Draft`/`UnderRevision`, PATCH bản đã ban hành → **409 VERSION_IMMUTABLE (BR-03)**.
- **FE**: `EditorPage` 2 mode — `/editor/new` (tạo Draft) + `/editor/:versionId` (sửa → Lưu → Gửi duyệt) + `AUTHOR_ROLES` + link AppShell.
- Nội dung vẫn ở cột `content_markdown` (DB) — **Git store hoãn** theo quyết định.
- **Test**: BE +3 (get/patch draft, immutable 409, READER 403) · FE +3.

### ✅ Task 5 — Audit `actor_id` + partition `audit_logs`
- **5a — map actor_id**: `AuditService.LogAsync` resolve/**upsert** User theo `sso_subject`, set `actor_id`; action string **sạch** (bỏ `by <sub>`); scheduler → `actor_id = null` (system). Fix `Document.Id` gán client-side để `document_id` trong audit đúng (trước là `Guid.Empty`).
- **5b — partition theo tháng**: migration `PartitionAuditLogs` chuyển `audit_logs` sang **declarative partitioning `RANGE(at)`**, PK `(id, at)`, tạo bảng mới → copy 122 rows → swap; partition `2026_07/08` + **DEFAULT**; **Quartz `AuditPartitionMaintenanceJob`** (StartNow + daily, tạo trước 2 tháng).
- **Test**: BE +2 (actor_id map + action sạch; partition service idempotent).

---

## 2. Tính năng hoàn thành (features)

- [x] Chạy full-stack thật local (không cần mock) — `dev:real`
- [x] **Hàng đợi duyệt** cho Approver/QA Lead — duyệt & từ chối kèm nhận xét
- [x] **Tìm kiếm full-text** tài liệu Effective có snippet
- [x] **Soạn / sửa tài liệu** (draft) + gửi duyệt — tôn trọng bất biến BR-03
- [x] **Audit trail** truy vết đúng người (actor_id) + sẵn sàng scale (partition tháng)

> [!info] Vòng đời DOC end-to-end đã khép kín
> Soạn (S7) → gửi duyệt → Hàng đợi duyệt (S9) → publish → auto-Effective (WF-03) → đọc + ack (S4) → tìm (S3) → audit. Tất cả chạy E2E thật trên `:5048`.

---

## 3. Quyết định (Decisions)

> [!note] DEC-1 — Port dev BE = `:5048` (không phải `:8080`)
> `dotnet run` **luôn** dùng `launchSettings.json` (http profile = 5048), bỏ qua `ASPNETCORE_URLS`. `:8080` chỉ đúng khi chạy DLL trực tiếp & đang bị **kind cluster `obs-ref`** chiếm trên máy này. → Proxy Vite mặc định trỏ `:5048`, override được qua `VITE_API_PROXY_TARGET`.

> [!note] DEC-2 — Giữ `npm run dev` = mock, real mode qua `dev:real`
> Không đổi default sang real → giữ trải nghiệm dev offline, non-breaking.

> [!note] DEC-3 — Review queue: endpoint riêng `GET /v1/review-queue`
> Chọn endpoint chuyên biệt (rõ nghĩa nghiệp vụ, dễ mở rộng filter) thay vì `/versions?status=`. `author` **hoãn** (chưa có cột, chờ Task 5 actor_id) → dùng `submitted_at = version.updated_at`.

> [!note] DEC-4 — Editor trước, Git store hoãn
> Làm `PATCH /versions/{id}/content` với `content_markdown` (DB) hiện có; **Git store (LibGit2Sharp) tách task riêng** — Hard Stop (thêm dependency + hạ tầng repo).

> [!note] DEC-5 — Partition đầy đủ + Quartz maintenance
> Chọn phương án đầy đủ: bảng partitioned + DEFAULT + job tự tạo partition tháng kế (thay vì minimal / defer). Đúng SDD §2.4.

> [!note] DEC-6 — Scheduler audit → actor_id null
> Hành động hệ thống (auto-Effective) không tạo user giả `"scheduler"`; action string `version.auto_effective` đã đủ ngữ nghĩa.

---

## 4. Gotchas (bài học kỹ thuật)

> [!warning] GOT-1 — EF `SqlQuery`/`SqlQueryRaw` **compose INSERT thành subquery** → fail
> `Database.SqlQuery<T>($"INSERT … RETURNING …")` bị bọc thành `SELECT … FROM (<sql>)` → lỗi. Dùng **`ExecuteSqlInterpolatedAsync`** (chạy trực tiếp) cho `INSERT … ON CONFLICT DO NOTHING`, rồi `SELECT id` riêng.

> [!warning] GOT-2 — Race unique `sso_subject` khi tạo user trong audit
> Test chạy **song song** nhiều class cùng `sub="tester@fpt"` → 2 request cùng first-action → vỡ `uq_users_sso_subject` (500). Đây là **bug thật ở prod**, không chỉ ở test. Fix bằng `INSERT … ON CONFLICT (sso_subject) DO NOTHING` (race-safe), **không** premature-commit pending changes của caller.

> [!warning] GOT-3 — Postgres partition & DEFAULT
> Không convert bảng thường → partitioned tại chỗ (phải tạo bảng mới → copy → swap). PK **phải chứa** cột partition → `(id, at)`. Tạo partition cho tháng **đã có rows trong DEFAULT** sẽ **fail** → phải tạo **trước** (job maintenance chạy `StartNow` + daily, `monthsAhead=2`).

> [!warning] GOT-4 — `audit_logs.document_id` từng là `Guid.Empty`
> Audit `document.created` được log **trước** `SaveChanges` khi `doc.Id` chưa sinh (DB gen). Fix: gán `Document.Id = Guid.NewGuid()` client-side.

> [!warning] GOT-5 — vitest `vi.mock` hoisting
> Biến top-level tham chiếu trong factory `vi.mock` bị hoist → `ReferenceError`. Đưa mock fn vào `vi.hoisted({...})`.

> [!warning] GOT-6 — cwd kẹt & lock DLL (đã biết từ trước, tái xác nhận)
> Chạy `npm --prefix apps/web …` từ repo root để không kẹt cwd. BE `dotnet run` nền **lock `Qgp.Api.dll`** → phải **stop BE trước khi `dotnet test`** (rebuild).
> Không cài `@testing-library/user-event` (chưa có trong deps) → test dùng `fireEvent`.

> [!tip] Query partition của 1 row
> `SELECT a.tableoid::regclass FROM qgp.audit_logs a …` (phải **table-qualified** `a.tableoid`).

---

## 5. Cần cải tiến / nợ kỹ thuật (Tech debt)

- [ ] **Git store thật** cho nội dung version (LibGit2Sharp) — thay `content_markdown` DB tạm; commit-on-publish (BR-03). *Chặn E1.6b/E1.11.*
- [ ] **Editor S7**: chưa có **live preview** markdown (chỉ textarea); chưa có metadata form đầy đủ (tags, audience_roles, classification).
- [ ] **Review queue**: `author` (người soạn) chưa hiển thị — cần cột/relationship khi có nhu cầu.
- [ ] **S3 search**: mất thông tin `type`/`tags`/non-Effective so với list cũ; `listDocuments`/`/documents` giờ là dead surface ở FE.
- [ ] **AuditService**: mỗi lần tạo user mới tốn 1 round-trip upsert; cân nhắc ensure-user ở tầng auth (login) để giảm.
- [ ] **EF split-query warning** (Include nhiều collection) — cân nhắc `AsSplitQuery()` cho query nặng.
- [ ] **`content_markdown` maxlength/validation** & sanitize phía BE trước khi render.
- [ ] Partition: chưa có **retention/detach** partition cũ (NFR-03 giữ ≥2 năm) — cần policy drop/archive.
- [ ] Test **integration cần Docker up** (Postgres) — chưa có Testcontainers để tự spin.

---

## 6. Trạng thái kiểm thử

| Hạng mục | Kết quả |
|---|---|
| BE unit/integration | **24/24 pass** |
| FE vitest | **19/19 pass** |
| FE typecheck (`tsc -b`) | ✅ sạch |
| FE build (vite) | ✅ |
| OpenAPI (Redocly lint) | ✅ valid |
| Migration `PartitionAuditLogs` | ✅ áp DB thật, 122 rows bảo toàn |
| E2E real (`:5048` qua proxy) | ✅ tất cả 5 task |

**API mới (additive, non-breaking)**: `GET /v1/review-queue`, `GET`+`PATCH /v1/versions/{id}/content` — đều regen FE client từ `openapi.yaml` (single source).

---

## 7. Bước tiếp theo (để hoàn tất MVP Phase 1)

Còn ~5 epic để đóng MVP:
1. **E1.5** — Version history + **diff** (S6) *(openapi đã có `/documents/{doc_id}/diff`)*
2. **E1.6b** — **Publish dialog S8** + **Git store** *(cần chốt lib trước)*
3. **E1.10** — **Audit log UI (S16)** + ADM roles/tags (S17) *(BE audit đã sẵn)*
4. **E1.9** — **Feedback** (S13 popover + S14 triage)
5. **E1.8** — **KB / Wiki** (S10)

Sau đó: **Phase 2** (REC/Onboarding/Reports/Notifications) → **Phase 3** (RAG + BOT) → **hardening** (OIDC thật, GitLab CI, Vault, OTel).

> [!question] Quyết định cần chốt sớm
> **Git store** (LibGit2Sharp vs giữ DB) — chặn E1.6b & E1.11.

---

*Nguồn chi tiết: `.claude/memory/project_state.yaml` + auto-memory `qgp-prd-baseline.md`. Liên quan: [[QGP BUILD_PLAN]] · [[QGP SDD v1.0]] · [[QGP PRD v1.0]].*
