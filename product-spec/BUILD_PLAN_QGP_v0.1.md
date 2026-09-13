---
title: "Build Plan — Quality Governance Portal (QGP)"
doc_id: BUILD-QGP-001
version: "0.1"
status: DRAFT
owner: "SA/TL + BA (PhucDN7)"
sources:
  - "Design: Claude Design project 2bacf013 — Quality Governance Portal.dc.html (Minimal UI)"
  - "PRD-QGP-001 v1.0 · SDD-QGP-001 v1.0 · ADR-0001..0013"
  - "API: product-spec/api/openapi.yaml, qgp-rag.openapi.yaml"
  - "Codegen: product-spec/generated/{qgp-api-server, qgp-rag-client}"
date_created: 2026-07-11
---

# Build Plan — Quality Governance Portal (QGP)

> Kế hoạch hiện thực hoá sản phẩm từ **design (Minimal UI, 14 màn)** + **PRD/SDD/ADR** + **OpenAPI**.
> Nguyên tắc: **dựng lại bằng React thật** (không ship runtime prototype .dc.html), tái dùng **token +
> component Minimal UI** và **client sinh từ OpenAPI**, nối vào **BE .NET** đã scaffold.

---

## 1. Từ design → sản phẩm: chiến lược

| Thành phần design (Claude Design) | Dùng thế nào khi build |
|---|---|
| colors_and_type.css (tokens Minimal) | Nguồn **design tokens** → nạp FE (CSS vars) + map Tailwind |
| _ds/.../ui_kits/dashboard (React kit) | Điểm xuất phát cho **component library** (Button/Card/Input/Table/Sidebar/Chip) |
| 14 màn .dc.html (sc-if/sc-for/{{}}) | **Đặc tả UI** cho từng route React — copy layout, thay data-binding bằng React state + API |
| qgp-strings.js | **i18n VN** — nguồn chuỗi (nhãn, badge, lỗi) → strings.ts |
| qgp-icons.json | Bộ icon Iconify solar dùng trong app |
| Nội dung QA nhúng (8-step, QG1/2) | Seed corpus mẫu cho dev/staging |

**KHÔNG** ship support.js/_ds_bundle.js (runtime prototype). Design = spec thị giác + tokens; app là React.

---

## 2. Kiến trúc mục tiêu (nhắc lại — SDD)

React (Vite+TS) --REST /v1--> qgp-api (.NET 8) --SQL--> Postgres 16 (+pgvector); qgp-api --HTTP--> Meilisearch;
--RESP--> Redis; --REST--> qgp-rag (Python, P3) --> LLM. Auth OIDC (SSO). Deploy docker-compose/Ubuntu; CI
GitLab; observability OpenTelemetry (SDD §1.5, ADR-0011/0013).

---

## 3. Repo layout đề xuất (monorepo)

- /apps/web — React + Vite + TS (FE): /src/app (routes 14 màn), /components (DS Minimal), /api (client từ openapi.yaml), /theme (tokens.css + tailwind), /i18n (strings.ts)
- /apps/api — qgp-api (.NET 8) từ generated/qgp-api-server + services
- /apps/rag — qgp-rag (Python FastAPI, P3)
- /db — EF Core migrations (DDL SDD §2)
- /deploy — docker-compose, nginx, otel, gitlab-ci.yml
- /design — export design + tokens (tham chiếu)

---

## 4. Ánh xạ 14 màn design → route + API

| Màn design (H1) | Route | Screen | API (operationId) | Phase |
|---|---|---|---|---|
| Chào mừng trở lại | /login | S1 | (OIDC) | P1 |
| Xin chào {{firstName}} | / | S2 home + gợi ý | getRecommendations | P2 (P1 rút gọn) |
| Tài liệu | /documents | S3 | listDocuments, search | P1 |
| {{doc.title}} | /documents/:docId | S4/S5 badge + cảnh báo non-Effective | getDocument, acknowledgeVersion | P1 |
| Lịch sử phiên bản | /documents/:docId/history | S6 | listVersions, getDiff | P1 |
| Soạn / sửa tài liệu | /documents/:docId/edit | S7 + PublishDialog (S8) | createDocument, createVersion, publishVersion | P1 |
| Hàng đợi duyệt | /review | S9 | submitVersion, approveVersion | P1 |
| Wiki · KB | /kb | S10 | (KB) | P1 |
| Onboarding | /onboarding | S12 | (ONB) | P2 |
| Xử lý feedback | /feedback | S14 | triageFeedback | P1/P2 |
| Báo cáo quản trị | /reports | S15 | getIssuanceReport, getComplianceReport | P2 |
| Audit log | /audit | S16 | listAudit | P1 |
| Quản trị | /admin | S17 | (ADM) | P1/P2 |
| Trợ lý QGP | /assistant | S18 | askAssistant | P3 |

Component (không phải route): FeedbackPopover (S13 in-context trên S4), NonEffectiveBanner (S5), Notifications (S19, bell).

---

## 5. Nền tảng dùng lại (đã có)
- **API contract** openapi.yaml (0 lỗi) → sinh **TS client** FE; đã có **.NET server stub** + **.NET client qgp-rag**.
- **Tokens + DS** colors_and_type.css + kit.css.
- **Data model** SDD §2 → EF Core migrations.
- **BR & flows** BR-01..11, WF-01..07, state machine §4.3 → logic BE.

---

## 6. Kế hoạch theo phase (epics)

### PHASE 0 — Foundation
- E0.1 Monorepo + docker-compose (postgres/meili/redis/nginx) + CI skeleton.
- E0.2 FE scaffold Vite+TS+Router; nạp tokens + Tailwind map; Inter; Iconify.
- E0.3 Component library Minimal: Button, Card, Input/Select/Textarea, Table (dashed divider), Chip, Badge, Avatar, Sidebar (280/88), Header (80/64), Dialog, Toast, Skeleton, EmptyState, ErrorState + StatusBadge (7 trạng thái, soft), EffectiveDateBadge, NonEffectiveBanner, DocumentCard, ReasonChip.
- E0.4 API client: openapi-typescript từ openapi.yaml; wrapper fetch (auth, error→toast theo code).
- E0.5 i18n strings.ts từ qgp-strings.js.
- E0.6 BE scaffold từ generated/qgp-api-server; DI, EF Core+Npgsql, Serilog, FluentValidation, Polly, Quartz.NET; healthz; OTel.
- E0.7 DB: EF migrations 12 bảng + partial-unique BR-02 + CHECK BR-07 + partition audit; seed roles.
- E0.8 Auth OIDC + RBAC policies §10.1; FE login (S1).

### PHASE 1 — MVP (DOC + KB + FBK + ADM)
- E1.1 DOC lifecycle (WF-01): create/submit/approve/publish + VersionService + AuditService.
- E1.2 Scheduler WF-03 (auto-Effective + supersede, idempotent) — SDD §5.1.
- E1.3 Document view S4/S5: Markdown render + sanitize, StatusBadge, EffectiveDateBadge, NonEffectiveBanner, TOC, ack, related.
- E1.4 Search S3: Meilisearch index (chỉ Effective) + /search + filter + skeleton.
- E1.5 Version history + diff S6.
- E1.6 Editor + Publish dialog S7/S8: MetadataForm (doc_id immutable, enum, audience_roles, tags), MarkdownEditor(preview), PublishDialog validate BR-04/07.
- E1.7 Review queue S9: approve/reject + comment.
- E1.8 KB S10: transclude DOC (version/status) + superseded warning.
- E1.9 Feedback S13/S14: in-context popover (auto-context) + triage board.
- E1.10 ADM S17 core: RBAC/roles, taxonomy/tags; Audit log S16.
- E1.11 Migrate corpus mẫu (8-step, QG1/2) vào Git + seed.

### PHASE 2 — Vận hành & đo (ONB + RPT + REC)
- E2.1 REC engine (rules-based, explainable BR-11) + panel S2 + "Bắt đầu từ đâu" S11.
- E2.2 ONB S12: learning path + checklist % + ack.
- E2.3 RPT dashboards S15 (aggregate, tách dữ liệu thô) + review-cycle reminders (DOC-F-08).
- E2.4 Notifications/subscription S19.

### PHASE 3 — AI (BOT)
- E3.1 qgp-rag Python (contract qgp-rag.openapi.yaml): ingest Effective→pgvector, /query, /index.
- E3.2 BOT chat S18: citation card, "không chắc", chuyển thành feedback; kill switch.

---

## 7. Sprint 1 (foundation) — task + DoD

| Task | DoD |
|---|---|
| T1 Monorepo + docker-compose up (pg/meili/redis) | docker compose up chạy; healthz xanh |
| T2 FE scaffold + tokens + Tailwind + Inter + Iconify | Render với token; StatusBadge demo đúng 7 màu |
| T3 Sinh TS client từ openapi.yaml | gen:api ra types + client; build FE pass |
| T4 DS components core | gallery render; khớp Minimal (radius/shadow/dashed) |
| T5 BE scaffold + EF migrations (12 bảng) + seed roles | dotnet ef database update OK; partial-unique BR-02 tồn tại |
| T6 OIDC + RBAC + FE login (S1) | Đăng nhập SSO → session theo role; chặn route ngoài quyền |
| T7 Vertical slice: Document view (S4) đọc Effective | GET /documents/{id} → render Markdown + StatusBadge + ack |

Sprint 1 = "đi xuyên" 1 lát cắt (auth → API → DB → UI) trên DS Minimal.

---

## 8. Ràng buộc & quy ước (governance)
- Enum/field FE+BE lấy từ openapi.yaml (single source).
- Bám 11 Business Rule (single Effective, immutability, Approved≠Effective, change_summary, reset ack...).
- Naming theo docs/ai/internal_rules/; commit Conventional + tag [AI]; MR compliance.
- A11y WCAG 2.2 AA (design có dark mode + tokens contrast).
- Test: unit (BR/state machine/REC), integration (Testcontainers), E2E (AC PRD §9.2 → TC).

---

## 9. Cần chốt trước khi code
| # | Cần chốt | Vì |
|---|---|---|
| Q1 | OIDC provider nội bộ (endpoint, client) | E0.8 |
| Q2 | Hạ tầng Ubuntu + GitLab (registry, runner) | E0.1, deploy |
| Q3 | Logo/brand thật | FE header |
| Q4 | Tailwind hay CSS thuần + tokens? (đề xuất Tailwind) | E0.2 |
| Q5 | Xác nhận Meilisearch (ADR-0009) | E1.4 |
| Q6 | Quy mô team + timeline | Chia sprint |

---

## 10. Rủi ro
- Corpus chưa sạch → BOT (P3) hoãn cuối P2.
- Non-dev thao tác Git → editor UI trừu tượng hoá Git (E1.6, R5 PRD).
- Lệch design ↔ enum spec → chốt §8 (dữ liệu là hợp đồng).

---

## 11. Bước tiếp ngay
1. Chốt Q1–Q6 (§9).
2. Scaffold Sprint 1 (E0.1–E0.4 + T7 vertical slice).
3. Tạo task tracker (epic E0/E1) theo pipeline repo nếu quản lý 2b.

> ⚠️ Shell working dir đang kẹt từ lượt trước (Bash/Edit/Write vướng hook). Trước khi scaffold nhiều file
> nên /clear hoặc mở session mới để khôi phục tool; hoặc tiếp tục qua PowerShell.

---

*BUILD-QGP-001 v0.1 · từ Design (Minimal UI, 14 màn) + PRD/SDD/ADR + OpenAPI · Nội bộ FPT ISC*
