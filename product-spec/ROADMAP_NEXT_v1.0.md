---
title: "QGP — Kế hoạch phát triển tiếp theo (Roadmap) v1.0"
doc_id: PLAN-QGP-NEXT-001
version: "1.0"
status: DRAFT
owner: "SA/TL (PhucDN7)"
sources:
  - "PRD-QGP-001 v1.0 (Approved)"
  - "SDD-QGP-001 v1.0 (IN_REVIEW)"
  - "BUILD_PLAN_QGP v0.1"
  - "DESIGN_BRIEF_QGP v0.2"
  - "Worklog 2026-07-25"
date_created: 2026-07-25
---

# QGP — Kế hoạch phát triển tiếp theo (Roadmap)

> Báo cáo dựa trên scan codebase thực tế (apps/api, apps/web) + PRD v1.0 + SDD v1.0 + BUILD_PLAN v0.1.
> Trạng thái hiện tại: **cuối Phase 1 (MVP) ~90% + phần lớn Phase 2 ~95%, Phase 3 (AI/BOT) 0%**.

---

## A. Trạng thái hiện tại (snapshot)

| Phase | Epic/Screen | Trạng thái |
|---|---|---|
| P0 Foundation | E0.1–E0.8 (monorepo, DS, client, BE, DB, auth) | ✅ Hoàn thành |
| P1 DOC | E1.1 lifecycle, E1.2 scheduler, E1.3 view, E1.5 diff, E1.7 review, E1.9 feedback, E1.10 admin/audit, Git store | ✅ Hoàn thành |
| P1 DOC | E1.6 editor (S7) | ⚠️ Phần lớn — thiếu metadata form đầy đủ + live preview |
| P1 KB | E1.8 KB S10 | ⚠️ Browse Effective, chưa transclude (AC-KB-01/03) |
| P1 | E1.4 search S3 | ✅ nhưng mất filter type/tags |
| P1 | E1.11 corpus migration | ❓ DevDataSeeder (QA-PROC-005), corpus thật chưa rõ |
| P2 REC | E2.1 (S2, S11) | ✅ Hoàn thành (BR-11 explainable) |
| P2 ONB | E2.2 (S12) | ✅ Hoàn thành |
| P2 RPT | E2.3 (S15 + reminder) | ✅ Hoàn thành |
| P2 Notif | E2.4 (S19) | ✅ Hoàn thành |
| P3 BOT | E3.1, E3.2 (S18) | ❌ Chưa bắt đầu (đúng roadmap) |
| Hardening | Redis, Polly, rate limit, sanitizer, partition retention | ❌ Chưa wire |

---

## B. Kế hoạch chi tiết theo milestone

### MILESTONE M1 — Hoàn tất Phase 1 (MVP) [mục tiêu: đóng MVP]

> Mục tiêu: chốt các đầu việc dở của P1 để corpus P1 sẵn sàng migrate và MVP "production-ready" cho pilot nội bộ.

#### M1.1 — Editor S7 metadata form đầy đủ + live preview (E1.6)
**Vì sao**: EditorPage hiện chỉ có docId/title/type/content/mandatory_ack; thiếu audience_roles (nguồn REC BR-11), tags, classification, next_review_date — metadata cần cho REC/health report/AC-DOC-15. Live preview markdown (SDD §6.4 sanitizer) chưa có.
**Cần làm**:
- BE: `UpdateContentRequest` đã có; thêm endpoint/field cập nhật `audience_roles`, `tags`, `classification`, `next_review_date` trên document (nếu chưa có endpoint set riêng — `/documents/{docId}/audience` PUT đã có, cần thêm tags/classification/next_review).
- FE EditorPage: thêm MetadataForm component (audience_roles multiselect, tags input, classification select, next_review_date date picker) + MarkdownEditor live preview (Markdig sanitize phía BE — SDD §6.4).
- Test: BE validation metadata (BR audience role hợp lệ), FE render preview.
**AC**: S7 có form đầy đủ (doc_id immutable, type, classification, audience_roles, tags, mandatory_ack, next_review_date) + preview song song.
**BR/AC liên quan**: AC-DOC-01 (chặn publish thiếu metadata), BR-11 (audience_roles), REC (nguồn gợi ý).

#### M1.2 — KB Wiki S10 transclude (E1.8)
**Vì sao**: KbPage hiện là browse/search tài liệu Effective; chưa transclude (nhúng trang KB hiển thị version+status — AC-KB-01) và cảnh báo Superseded (AC-KB-03).
**Cần làm**:
- BE: (nếu KB là trang riêng) cần model KB page (title, body markdown, nhúng doc_ref); hoặc dùng `getDocument` + render. Cảnh báo Superseded: DocumentView đã có NonEffectiveBanner — áp vào KB.
- FE KbPage: thêm view trang KB nhúng DOC (version + status badge), cảnh báo nếu tài liệu nguồn bị Superseded + link Effective.
- Test: AC-KB-01 (transclude hiển thị version+status), AC-KB-03 (cảnh báo Superseded).
**AC**: mở trang KB → thấy version + trạng thái hiện tại; tài liệu Superseded → cảnh báo + link Effective.

#### M1.3 — Audit log S16 filter UI (E1.10)
**Vì sao**: BE `/audit` đã có param filter (document_id, from, to) nhưng FE AuditPage chưa expose filter.
**Cần làm**:
- FE AuditPage: thêm filter theo document_id, actor, khoảng thời gian (from/to date), action type; phân trang cursor.
- Test: filter trả đúng subset.
**AC**: AC-ADM-02 (truy vết theo tài liệu/người/thời gian).

#### M1.4 — Search S3 filter (E1.4)
**Vì sao**: Search Meilisearch chỉ trả Effective (đúng BR-06) nhưng mất filter type/tags so với list cũ.
**Cần làm**:
- BE `/search`: thêm filterable attributes (type, tags) trong Meilisearch index + query params (đã có type/tag param nhưng cần verify filter Meili).
- FE DocumentsPage: FilterChips (type, tags) + sort.
- Test: AC-KB-02 (filter tag/loại/phân hệ, < 1s).
**AC**: AC-KB-02.

#### M1.5 — Corpus migration P1 (E1.11)
**Vì sao**: DevDataSeeder chỉ seed 1 doc (QA-PROC-005); P1 cần migrate corpus thật (8-step, QG1/2, Naming Convention) vào Git + DB.
**Cần làm**:
- Script idempotent per tài liệu (inventory → convert Markdown+frontmatter → assign doc_id/version/status/effective/classification/audience_roles → commit Git → seed DB → build Meili index → verify BR-02).
- Verify invariant BR-02 (1 Effective/doc) sau migrate.
**AC**: G1 (100% tài liệu QA có đúng 1 Effective) đo được sau migrate.

#### M1.6 — Review queue hiển thị author
**Vì sao**: ReviewQueueItemDto dùng `submitted_at = updated_at`, chưa có cột author (worklog DEC-3).
**Cần làm**:
- BE: thêm `author` (sub/năm) vào review-queue (join User qua audit `version.created` hoặc thêm cột `created_by` trên version).
- FE: hiển thị author trong ReviewQueuePage.
**AC**: Approver thấy ai soạn.

---

### MILESTONE M2 — Hardening & Production-readiness (NFR)

> Mục tiêu: đáp ứng NFR (reliability, security, performance) trước khi pilot nội bộ.

#### M2.1 — Redis caching (SDD §7.3)
**Cần làm**:
- Wire Redis: cache REC per-user 5m (invalidate on new Effective/major/ack), cache search phổ biến 60s, idempotency-key 24h cho POST unsafe.
- Test: cache hit/miss, invalidation.
**Liên quan**: SDD §7.3, REC-F-08 (invalidate).

#### M2.2 — Polly circuit breaker + retry (SDD §8.2/8.3)
**Cần làm**:
- Polly cho qgp-api → Meilisearch (timeout 2s, retry 2 exp, circuit breaker) + qgp-rag (khi P3) + SSO introspection (3s).
- Test: failover Meili → fallback Postgres FTS (SDD §8.1).
**Liên quan**: SDD §8.1/8.2/8.3.

#### M2.3 — Rate limit + HTML sanitizer (SDD §6.4)
**Cần làm**:
- Rate limit Nginx + API (feedback/search spam — SDD STRIDE DoS).
- Sanitize Markdown render (Markdig + HTML sanitizer chống XSS) cho DocumentView + KB transclude + BOT citation (P3).
- Validate frontmatter CI (doc_id regex `^[A-Z]{2,}-[A-Z]{2,}-\d{3,}$`, version `^\d+\.\d+$).
**Liên quan**: SDD §6.4, BR-01.

#### M2.4 — Audit partition retention (NFR-03)
**Cần làm**:
- Quartz job/policy detach + archive partition > 24 tháng (SDD §2.4/§2.5).
- Test: retention policy.
**Liên quan**: NFR-03, BR-09.

#### M2.5 — Testcontainers + E2E AC
**Cần làm**:
- Thay integration test phụ thuộc Postgres up thủ công → Testcontainers (spin Postgres + Meili per test).
- E2E map AC PRD §9.2 (AC-DOC-01..17, AC-KB-01..04, AC-ONB-01..03, AC-RPT-01..04, AC-FBK-01..04, AC-BOT-01..04) → TC.
- Coverage mục tiêu: unit ≥ 80% logic BR-01..11.
**Liên quan**: SDD §10.

#### M2.6 — Observability (SDD §9)
**Cần làm**:
- Serilog structured JSON (đã có ISC.Observability) — verify field chuẩn (correlation_id, duration_ms, action, doc_id).
- Metrics RED + USE (http_requests_total, http_request_duration, search_latency, scheduler_effective_transitions, rag_query_total [P3], db_pool_in_use).
- Alerts: SchedulerStalled (>26h), SearchSlow (>1s 5m), HighErrorRate (5xx >2%), RagCitationMissing (P3), PostgresDown.
**Liên quan**: SDD §9.1–9.4.

#### M2.7 — OIDC prod + secrets (Vault)
**Cần làm**:
- Chuyển Auth mode Dev → Oidc prod (Keycloak realm thật, role map, claims transformation).
- Secrets Vault (OIDC secret, DB creds, Meili/Redis, LLM key [P3]).
- CI/CD GitLab pipeline (validate markdownlint + frontmatter + BR lint → build → test → package docker → staging → prod).
**Liên quan**: SDD §6.5, §11.2, ADR-0011/0012.

---

### MILESTONE M3 — Phase 2 hoàn thiện + đo lường

> P2 phần lớn đã xong; đây là đầu việc "đánh bóng" + đo lường adoption.

#### M3.1 — Review cycle (AC-DOC-15)
**Trạng thái**: ReviewReminderJob đã có. Verify: nhắc owner trước hạn `next_review_date`, đánh dấu "quá hạn" trong RPT.
**Cần làm**: verify RPT-F-02 (sức khoẻ: quá hạn/sắp hạn) dùng `next_review_date`; thêm UI badge "quá hạn review" trên document.

#### M3.2 — RPT tách dashboard vs dữ liệu thô (C8)
**Cần làm**: dashboard > ~10 dòng → tách màn/sheet (DESIGN_BRIEF C8, PRD §7). Verify ReportsPage có chế độ xem raw data riêng.

#### M3.3 — Metrics adoption (OD-3, BR-10)
**Cần làm**: product analytics aggregate (doc_viewed, doc_acknowledged, feedback_submitted, search_performed, recommendation_shown) — không lộ cá nhân (BR-10).

---

### MILESTONE M4 — Phase 3 (AI/BOT)

> Điều kiện vào: corpus P1 đã sạch & ổn định (G1 đạt). SDD §4.6, BOT-F-01..07, AC-BOT-01..04.

#### M4.1 — qgp-rag Python service (E3.1)
**Cần làm**:
- `apps/rag` — Python FastAPI + LlamaIndex, contract `qgp-rag.openapi.yaml` (/query, /index/upsert).
- Ingest: chỉ tài liệu Effective (BR-06) → pgvector embeddings.
- Retrieval: vector search chỉ Effective, trả citations (doc_id + version + effective_date + link).
- Kill switch: `bot.killSwitch` → 503 (SDD §11.3, BOT-F-05).
- Defense-in-depth: API verify citations != rỗng (SDD §4.6).
- pgvector (ADR-0008) — đã có trong Postgres.
**AC**: AC-BOT-01 (chỉ index Effective), AC-BOT-02 (citation bắt buộc), AC-BOT-03 ("không chắc"), AC-BOT-04 (re-index khi đổi trạng thái).

#### M4.2 — BOT chat S18 (E3.2)
**Cần làm**:
- FE `/assistant` page (S18): ChatMessage + CitationCard, trạng thái "không chắc", nút chuyển thành feedback (BOT-F-06).
- BE: RagGateway client gọi qgp-rag, timeout 10s + circuit breaker.
- Re-index job: khi tài liệu đổi trạng thái (Effective→Superseded) → POST /index/upsert/delete.
- Feature flag `feature.bot` (off ở P1/P2).
**AC**: UC-17/18, BOT-F-01..07.

#### M4.3 — RAG observability + alert
**Cần làm**: metric `rag_query_total` (status answered/insufficient), alert RagCitationMissing (answered mà citations rỗng → kill BOT), BOT-F-07 log câu hỏi không trả lời được.

---

## C. Thứ tự ưu tiên đề xuất

| Thứ tự | Milestone | Lý do |
|---|---|---|
| 1 | **M1.1–M1.6** (đóng P1) | MVP chưa khép kín → không pilot được; G1 phụ thuộc corpus + metadata |
| 2 | **M2.1–M2.7** (hardening) | Pilot nội bộ cần OIDC thật, caching, rate limit, observability |
| 3 | **M3.1–M3.3** (P2 đánh bóng) | Đo adoption + RPT chính xác |
| 4 | **M4.1–M4.3** (P3 BOT) | Chỉ khi G1 đạt (corpus sạch) — SDD §8.1 cảnh báo P3 hoãn |

> **Hard stop cho P3**: corpus chưa sạch (G1 < 100%) → KHÔNG bắt đầu M4 (BOT index sai → hallucinate).

---

## D. Lưu ý quan trọng (ràng buộc phải tuân thủ)

1. **Single source of truth enum/field**: FE + BE bám `openapi.yaml` (BUILD_PLAN §8). Không tự đặt trạng thái/loại mới ngoài spec.
2. **BR-01..11 phải enforce ở mọi endpoint** (DB constraint + logic + test). Kiểm tra lại khi thêm metadata form (M1.1).
3. **Approved ≠ Effective** (WF-03 scheduler) — không bao giờ hiển thị Approved là "đang áp dụng".
4. **BR-06 chỉ Effective được index/search** — M4.1 RAG cũng phải chỉ index Effective.
5. **Audit partition** — M2.4 retention ≥ 2 năm (NFR-03).
6. **Metric truy cập aggregate** (BR-10, OD-3) — M3.3 không lộ per-user.
7. **Git store đã xong** (LibGit2GitContentStore) — M1.1 dùng cho metadata, không revert về DB-only.
8. **Idempotency-Key** (SDD §3.1) — M2.1 wire cho mọi POST/PATCH unsafe.

---

*PLAN-QGP-NEXT-001 v1.0 · map từ PRD v1.0 + SDD v1.0 + BUILD_PLAN v0.1 + scan codebase · Nội bộ FPT ISC*
