# Kế hoạch công việc tiếp theo — QMS Governance Platform (QGP)

> Ngày lập: 2026-09-13
> Nguồn: codevista-review-qgp.md, .claude/memory/task_tracker.yaml, .claude/memory/project_state.yaml, .claude/memory/schema_snapshot.yaml, apps/api/, apps/web/ (file:số dòng)

---

## Danh sách công việc (sắp theo thứ tự ưu tiên)

### Ưu tiên 0 — Điền docs/ai/PROJECT_MAP.md (meta, không động code)

**Lý do chọn ưu tiên 0:** `docs/ai/PROJECT_MAP.md` hiện vẫn là template TODO (toàn file 119 dòng, mọi mục đều `<vd: ...>` placeholder). Đây là file **mandatory eager-load** ở Phase 0 (`CLAUDE.md:15`: "Đọc `@docs/ai/PROJECT_MAP.md`"). Mọi AI agent session đều đọc file này đầu tiên — nếu để template, agent không biết stack/build/test commands/folder structure. Việc này không đổi code, không rủi ro, làm nhanh.

**File sẽ sửa:**

1. **`docs/ai/PROJECT_MAP.md`** — điền toàn bộ nội dung thực (thay placeholder):
   - **Tech Stack:** .NET 8 ASP.NET Core, PostgreSQL 16 (+pgvector), Meilisearch, Redis, Quartz.NET, LibGit2Sharp, ISC.Observability · React 18 + Vite 5 + TypeScript 5.6, Tailwind 3.4, react-oidc-context · xunit + WebApplicationFactory (BE), Vitest + Testing Library (FE)
   - **Folder Structure:** `apps/api/` (BE), `apps/web/` (FE), `product-spec/` (PRD/SDD/ADR), `docs/ai/` (rules), `.claude/memory/` (state), `deploy/` (docker-compose)
   - **Key Modules:** auth (`apps/api/src/Qgp.Api/Auth/`), application (19 services `apps/api/src/Qgp.Api/Application/`), infrastructure (persistence/search/scheduling/git `apps/api/src/Qgp.Api/Infrastructure/`), api (`apps/api/src/Qgp.Api/Api/V1Endpoints.cs`), domain (`apps/api/src/Qgp.Api/Domain/`)
   - **Important Files:** `CLAUDE.md`, `apps/api/src/Qgp.Api/Program.cs`, `apps/web/src/router.tsx`, `apps/web/package.json`, `apps/api/src/Qgp.Api/appsettings.json`, `deploy/.env.prod.example`
   - **Build/Run/Test commands:** `cd apps/api && dotnet build`, `cd apps/api && dotnet test`, `cd apps/web && npm run build`, `cd apps/web && npm run typecheck`, `cd apps/web && npm run test`, `cd apps/web && npm run gen:api`
   - **Architectural Decisions:** ADR-0001 (docs-as-code), ADR-0006 (.NET), ADR-0008 (Postgres+pgvector), ADR-0009 (Meilisearch), ADR-0010 (React), ADR-0014 (lean envelope), ADR-0015 (Git content store)
   - **Environments:** local (docker-compose), staging/prod (docker-compose.prod)
   - **What NOT to touch:** `apps/api/src/Qgp.Api/Infrastructure/Persistence/Migrations/` (chỉ tạo migration mới), `.claude/memory/` (chỉ Phase 5 ghi), `product-spec/api/openapi.yaml` (chỉ cập nhật khi API đổi)

**Cách kiểm chứng:**
1. **Visual check:** `cat docs/ai/PROJECT_MAP.md | grep -c '<vd:'` — kết quả mong đợi: 0 (không còn placeholder)
2. **Phase 0 verify:** mở session Claude Code mới → Phase 0 đọc `@docs/ai/PROJECT_MAP.md` → hiển thị stack/build/test commands đúng (không còn "TODO khi customize")
3. **CLAUDE.md reference verify:** `CLAUDE.md:15` ghi "Đọc `@docs/ai/PROJECT_MAP.md`" → file phải có nội dung thật, không template

**Rủi ro và ảnh hưởng:**
- **Rủi ro THẤP:** chỉ sửa 1 file markdown, không đổi code, không đổi build, không đổi test.
- **Ảnh hưởng phần đang chạy:** không. AI agent đọc file này ở Phase 0 nhưng nội dung chỉ tham khảo, không affect runtime.

---

### Ưu tiên 1 — b1c1: UI sửa audience per-document (DocumentPage)


**Lý do chọn ưu tiên 1:** Đây là task `open` duy nhất trong `task_tracker.yaml:117` (id `b1c1`), BE + test đã xong, chỉ thiếu UI. Hoàn thành task này sẽ đóng toàn bộ Epic B. Mọi task khác (ops go-live, Phase 3 RAG) đều không chặn go-live hoặc là value-add, nên b1c1 là việc nhanh nhất có giá trị hoàn chỉnh.

**File sẽ sửa:**

1. **`apps/web/src/pages/DocumentPage.tsx`** (hiện 30 dòng) — thêm AudiencePanel component vào phần header (dòng 16-26), hiển thị khi user có role admin.config. Gọi `getDocAudience(docId)` để load, `setDocAudience(docId, audience)` để save. Dùng `useAuth()` từ `apps/web/src/auth/AuthContext.tsx` để check role (ADMIN_ROLES = QA_LEAD/ADMIN, `apps/web/src/auth/roles.ts:33`).

2. **File mới: `apps/web/src/features/audience/AudiencePanel.tsx`** — component UI hiển thị danh sách role hiện tại + form thêm/xóa role (checkbox hoặc multi-select). Gọi `getDocAudience`/`setDocAudience` từ `apps/web/src/api/client.ts` (đã có: `getDocAudience` dòng 469, `setDocAudience` dòng 475). Dùng `DocAudienceItem` type từ `apps/web/src/api/types.ts`. Dùng `strings` từ `apps/web/src/i18n/strings.ts` cho label tiếng Việt.

3. **File mới: `apps/web/src/features/audience/useAudience.ts`** — hook `useAudience(docId)` trả `{ audience, loading, error, save }`. Fetch `getDocAudience` on mount, `save` gọi `setDocAudience`. Dùng `AbortController` pattern (tiền lệ `useDocument.ts:33-47`, `useDocumentSearch.ts:14-29`).

**File KHÔNG sửa (BE + test đã xong):**
- `apps/api/src/Qgp.Api/Application/DocumentService.cs` — `GetAudienceAsync` (dòng 19) + `SetAudienceAsync` (dòng 22) đã có
- `apps/api/src/Qgp.Api/Api/V1Endpoints.cs` — `GET /v1/documents/{docId}/audience` (dòng 188-190, admin.config) + `PUT` (dòng 192-194, admin.config) đã có
- `apps/api/src/Qgp.Api/Contracts/Dtos.cs` — `DocAudienceItemDto` (dòng 251) + `SetDocAudienceRequest` (dòng 252) đã có
- `apps/web/src/api/client.ts` — `getDocAudience` + `setDocAudience` đã có
- `apps/web/src/api/types.ts` — `DocAudienceItem` type đã có
- `apps/web/src/api/mock.ts` — `mockGetDocAudience` + `mockSetDocAudience` đã có

**Thứ tự thực hiện (TDD-first theo CLAUDE.md Phase 2 dòng 112-122):**

**Bước a — Viết test TRƯỚC (red):**

File mới: **`apps/web/src/test/AudiencePanel.test.tsx`** — tối thiểu 3 test case:

1. **Test case 1 — Admin thấy được AudiencePanel:** render `DocumentPage` với mock `useAuth()` trả role = `ADMIN` → `getDocAudience` mock trả `[{role: 'READER', reason: 'Bắt buộc'}]` → assert AudiencePanel hiển thị, assert text "READER" + "Bắt buộc" có trong document. (RBAC: `roles.ts:33` ADMIN_ROLES = [QA_LEAD, ADMIN], `V1Endpoints.cs:190` admin.config)

2. **Test case 2 — Người thường KHÔNG thấy AudiencePanel:** render `DocumentPage` với mock `useAuth()` trả role = `READER` → assert AudiencePanel KHÔNG có trong document (queryByText `strings.audience.title` → null). (RBAC: READER ∉ ADMIN_ROLES)

3. **Test case 3 — Bấm lưu gửi đúng dữ liệu:** render `DocumentPage` với mock `useAuth()` trả role = `ADMIN` → mock `setDocAudience` → click nút "Lưu" → assert `setDocAudience` được gọi với đúng `docId` + mảng `DocAudienceItem[]` (từ `client.ts:475`, PUT body `{ audience }` per `Dtos.cs:252`). Verify mock `setDocAudience` nhận argument có shape `{ role: string, reason: string | null }[]`.

> Chạy test: `cd apps/web && npm run test -- AudiencePanel` — kết quả mong đợi: **3 test FAIL** (red) vì AudiencePanel + useAudience chưa tồn tại. Đây là xác nhận TDD-first đúng quy trình.

**Bước b — Implement (green):**

Tạo 3 file ở phần "File sẽ sửa" trên. Sau khi implement:

**Bước c — Chạy lại test (xác nhận green):**

**Cách kiểm chứng (sau khi implement):**

1. **Test FE (TDD green):** `cd apps/web && npm run test -- AudiencePanel` — kết quả mong đợi: **3/3 passing** (3 test case ở bước a giờ pass)
2. **Test FE toàn bộ:** `cd apps/web && npm run test` — kết quả mong đợi: 43/43 + 3 = 46/46 passing
3. **Typecheck FE:** `cd apps/web && npm run typecheck` — kết quả mong đợi: 0 error
4. **Build FE:** `cd apps/web && npm run build` — kết quả mong đợi: vite build thành công, không lỗi
5. **Test BE (không đổi, chạy confirm không regression):** `cd apps/api && dotnet test` — kết quả mong đợi: 67/67 passing
6. **Test BE DocAudienceTests (đã có):** `dotnet test --filter DocAudienceTests` — kết quả mong đợi: 2/2 passing (GetAudienceAsync + SetAudienceAsync)
7. **E2E thủ công (dev:real):** `cd apps/web && npm run dev:real` → đăng nhập admin → vào /documents/:docId → thấy AudiencePanel → load role hiện tại → sửa (thêm/xóa role) → save → verify PUT trả 200 + GET lại hiển thị đúng
8. **RBAC check manual:** đăng nhập READER → vào /documents/:docId → KHÔNG thấy AudiencePanel (chỉ ADMIN_ROLES mới thấy)

**Rủi ro và ảnh hưởng:**
- **Rủi ro thấp:** BE + test đã xong (DocAudienceTests 2/2), chỉ thêm UI. Không đổi DB, không đổi API contract, không đổi migration.
- **Rủi ro UI/UX:** `setDocAudience` PUT thay thế **toàn bộ** tập (`V1Endpoints.cs:192-194`, `DocumentService.cs:22` "thay thế tập cũ"). UI phải hiển thị warning "thay thế toàn bộ" để tránh user tưởng chỉ thêm 1 role.
- **Ảnh hưởng phần đang chạy:** Không phá feature nào đang chạy. `DocumentPage.tsx` hiện có 30 dòng (DocumentView + SubscribeButton + FeedbackButton), thêm AudiencePanel chỉ mở rộng, không đổi component hiện có.
- **RBAC:** Audience API yêu cầu `admin.config` (`V1Endpoints.cs:190,194` = `QgpPolicies.AdminConfig`). UI phải ẩn AudiencePanel khi user không có role QA_LEAD/ADMIN (dùng `useAuth()` + check `ADMIN_ROLES`).

---

### Ưu tiên 2 — Ops go-live (CSP nginx, ef migrate deploy, Keycloak hardening, Vault, Grafana)

**Lý do chọn ưu tiên 2:** Theo `project_state.yaml:63` (current_sprint), nhóm A (deployment) đã xong nhưng còn "việc nhỏ go-live": CSP nginx, ef migrate deploy, Vault thật, deploy Grafana/collector. Đây là blocker cho production go-live (không thể deploy prod an toàn nếu thiếu).

**File sẽ sửa/tạo:**

1. **`apps/web/nginx.conf`** — thêm Content-Security-Policy header (hiện chỉ có security headers cơ bản). Thêm `add_header Content-Security-Policy "default-src 'self'; ..."` cho SPA.
2. **`deploy/docker-compose.prod.yml`** — thêm service `otel-collector` (nhận OTLP từ api) + `grafana` (dashboard). Mapping env `OTEL_OTLP_ENDPOINT` từ `.env.prod`.
3. **`deploy/.env.prod.example`** — thêm biến cho Vault (nếu deploy Vault Agent) hoặc ghi rõ dùng file-secrets `/run/secrets`.
4. **`deploy/keycloak/realm-qgp.json`** — ghi rõ comment "DEV ONLY, PROD phải tạo realm riêng, KHÔNG import file này" (hiện đã có nhưng cần hardening guide).
5. **`deploy/DEPLOY.md`** — cập nhật section ef migrate deploy (chạy `dotnet ef database update` thủ công, không auto-migrate trong Production image) + Vault setup + Grafana/collector deploy.

**Cách kiểm chứng:**
1. **nginx config test:** `docker run --rm -v $(pwd)/apps/web/nginx.conf:/etc/nginx/conf.d/default.conf nginx:alpine nginx -t` — syntax OK
2. **docker-compose prod config:** `docker compose -f deploy/docker-compose.prod.yml config` — valid YAML, không lỗi
3. **ef migrate dry-run:** `cd apps/api && dotnet ef migrations script --idempotent --output migrate.sql` — review SQL, không có DROP/ALTER phá data
4. **Keycloak prod:** verify realm-qgp.json KHÔNG import ở prod (chỉ dev), tạo realm riêng qua Keycloak Admin CLI hoặc REST API
5. **OTel verify:** `docker compose -f deploy/docker-compose.prod.yml up api otel-collector grafana` → kiểm tra traces/logs nhận tại Grafana (query `{service.name="qgp-api"}`)
6. **Smoke E2E prod:** `curl -s https://api.qgp.example.com/healthz` → 200 'Healthy'

**Rủi ro và ảnh hưởng:**
- **Rủi ro CAO:** động chạm production env (Hard Stop theo `CLAUDE.md:248-253`). Cần user approve trước khi deploy.
- **ef migrate:** không auto-migrate trong Production image (`project_state.yaml` gotcha: "BE image Production → không auto-migrate, DEPLOY.md ghi rõ chạy ef tay"). Chạy `dotnet ef database update` thủ công trên prod DB.
- **CSP nginx:** nếu sai CSP → FE không load (script/style blocked). Test kỹ với `report-uri` và `report-only` trước khi enforce.
- **Keycloak prod:** realm-qgp.json chỉ DEV (user/pass mẫu Password123!). PROD phải tạo realm riêng, không import file này (`project_state.yaml` decision A1a: "realm-qgp.json chỉ DEV — PROD phải tạo realm riêng, KHÔNG import file này").
- **Vault thật:** hiện dùng file-secrets `/run/secrets` (AddKeyPerFile, ADR-0012). Nếu deploy Vault Agent → render file `/run/secrets/<Key>` (dùng `__` nesting).
- **Ảnh hưởng phần đang chạy:** không phá dev/staging. Chỉ ảnh hưởng prod env.

---

### Ưu tiên 3 — `/classify` lại (LOC threshold)

**Lý do chọn ưu tiên 3:** `project_state.yaml:6` ghi `pattern: A` (solo, ≤12k LOC), `loc_at_classification: 0` (chưa đo). Project đã có BE 67/67 tests + FE 43/43 tests + 19 services + 16 pages + 8 migrations. Nếu LOC > 12000 (next_review_threshold) → cần re-classify sang Pattern B (Scoped, 10-100k LOC) để có rule overlay phù hợp.

**File sẽ sửa:**
1. **`.claude/memory/project_state.yaml`** — cập nhật `pattern`, `classified_at`, `loc_at_classification`, `next_review_threshold` sau khi đo LOC.

**Cách kiểm chứng:**
1. **Đo LOC:** `git ls-files | xargs wc -l 2>/dev/null | tail -1` — kết quả mong đợi: số LOC tổng
2. **So sánh threshold:** nếu LOC > 12000 → chạy `/classify` để re-classify sang Pattern B
3. **Verify:** `cat .claude/memory/project_state.yaml | grep pattern` — phải hiển thị pattern mới

**Rủi ro và ảnh hưởng:**
- **Rủi ro THẤP:** chỉ đọc file + cập nhật YAML, không đổi code.
- **Ảnh hưởng:** nếu re-classify sang Pattern B → thêm rule overlay (`patterns/pattern-b-scoped/CLAUDE.md.overlay`), agent dispatch, role boundary, context budget. Không phá code đang chạy.

---

### Ưu tiên 4 — RAG/BOT S18 (Phase 3, value-add)

**Lý do chọn ưu tiên 4:** Theo `ROADMAP_NEXT_v1.0.md`, Phase 3 là value-add (RAG chatbot), không chặn go-live. ADR-0007 (`product-spec/adr/0007-rag-python-service.md`) quyết định RAG là Python service riêng. ADR-0004 (`0004-bot-corpus-effective-only.md`) quyết định RAG chỉ index Effective (BR-06).

**File sẽ tạo (chưa xác minh chi tiết, cần spec riêng):**
1. **`apps/rag/`** — Python service mới (FastAPI/Flask), riêng biệt với apps/api (.NET)
2. **`apps/rag/requirements.txt`** — dependencies (pgvector, langchain hoặc similar)
3. **`apps/rag/src/main.py`** — entry point, expose API cho FE chat
4. **`apps/rag/src/indexer.py`** — index Effective documents vào pgvector (BR-06, ADR-0004)
5. **`apps/rag/src/chat.py`** — RAG chatbot, trả lời từ Effective docs, cite sources, "not sure" khi không biết
6. **`apps/web/src/features/bot/`** — FE chat component

**Cách kiểm chứng (chưa xác minh chi tiết — cần spec riêng):**
1. **BE rag test:** `cd apps/rag && pytest` — test indexer + chat
2. **E2E:** FE chat → BE rag → trả lời từ Effective docs + cite source
3. **BR-06 verify:** query RAG với doc Draft → KHÔNG trả về (chỉ index Effective)
4. **Governed AI verify:** chat hỏi câu không có trong docs → trả "not sure" thay vì hallucinate

**Rủi ro và ảnh hưởng:**
- **Rủi ro CAO:** service mới (Python), cần infra riêng (Docker, CI, monitoring). ADR-0007 quyết định Python service riêng.
- **Ảnh hưởng:** không phá apps/api hoặc apps/web đang chạy. RAG service tách biệt, giao tiếp qua API.
- **Phụ thuộc:** cần pgvector (đã có trong PostgreSQL 16, ADR-0008), cần Effective documents đã index.
- **Hard Stop:** install dependency mới (cần user approve, `CLAUDE.md:252`).

---

### Ưu tiên 5 — Redis cache cho RecommendationService

**Lý do chọn ưu tiên 5:** `project_state.yaml` decision #13 ghi "DEFER Redis cache 5m" cho RecommendationService (SDD §5.2). Hiện tính mỗi request (không cache). Nếu tải cao, wire Redis + invalidate on {new Effective|major|ack}.

**File sẽ sửa:**
1. **`apps/api/src/Qgp.Api/Application/RecommendationService.cs`** — thêm cache layer (get từ Redis, set 5m TTL, invalidate trên event)
2. **`apps/api/src/Qgp.Api/Program.cs`** — thêm Redis connection (AddStackExchangeRedis hoặc tương tự)
3. **`apps/api/src/Qgp.Api/appsettings.json`** — thêm Redis config (Connection string, TTL)

**Cách kiểm chứng:**
1. **BE test:** `dotnet test --filter RecommendationTests` — 2/2 passing (không regression)
2. **Cache verify:** gọi GET /v1/recommendations 2 lần → lần 2 trả từ cache (verify qua Redis CLI `KEYS *recommendations*`)
3. **Invalidate verify:** publish doc mới → recommendation cache invalidate → lần 3 trả data mới

**Rủi ro và ảnh hưởng:**
- **Rủi ro THẤP-TRUNG:** Redis đã có container trong docker-compose (schema_snapshot.yaml: "localhost:6379"), chỉ wire code.
- **Ảnh hưởng:** không phá feature đang chạy (cache miss → fallback tính mỗi request như cũ).
- **Hard Stop:** install dependency mới (StackExchange.Redis NuGet, cần user approve).

---

### Ưu tiên 6 — Access report (RPT-F-03) event tracking thật

**Lý do chọn ưu tiên 6:** `schema_snapshot.yaml` internal_apis.reports.gotcha ghi "Access là PROXY theo ack, CHƯA có event lượt xem (doc_viewed/search_performed) để P3". Hiện `AccessReportDto` (`Dtos.cs:198-202`) dùng proxy theo ack. Phase 3 nên implement event tracking thật.

**File sẽ tạo/sửa:**
1. **Migration mới** — bảng `doc_events` (doc_id, event_type, user_id, at) — partition theo tháng như audit_logs
2. **`apps/api/src/Qgp.Api/Domain/Entities/`** — entity DocEvent
3. **`apps/api/src/Qgp.Api/Application/ReportService.cs`** — sửa GetAccessAsync dùng doc_events thay vì proxy ack
4. **`apps/api/src/Qgp.Api/Application/DocumentService.cs`** — hook index event khi GET /v1/documents/{docId} (doc_viewed) + GET /v1/search (search_performed)
5. **`apps/api/src/Qgp.Api/Infrastructure/Persistence/QgpDbContext.cs`** — DbSet DocEvent
6. **`product-spec/api/openapi.yaml`** — cập nhật AccessReportDto note (không còn proxy)

**Cách kiểm chứng:**
1. **BE test:** `dotnet test --filter ReportTests` — passing với data thật từ doc_events
2. **E2E:** GET /v1/documents/{docId} 3 lần → GET /v1/reports/access → TopEngaged hiển thị doc đó với count=3
3. **Privacy verify:** access report aggregate, không lộ hành vi cá nhân (SDD §6 privacy)

**Rủi ro và ảnh hưởng:**
- **Rủi ro TRUNG:** migration mới (additive, không phá data cũ), cần DB migration trên prod.
- **Ảnh hưởng:** GET /v1/documents/{docId} + GET /v1/search chậm hơn (thêm insert event). Nếu tải cao → cân nhắc async/batch.
- **Hard Stop:** schema migration (`CLAUDE.md:248`).

---

## Tóm tắt thứ tự ưu tiên

| # | Task | Loại | File chính | Test | Rủi ro |
|---|------|------|-----------|------|--------|
| 0 | Điền PROJECT_MAP.md | Meta (markdown) | docs/ai/PROJECT_MAP.md | grep -c '<vd:' = 0 | Thấp |
| 1 | b1c1 audience UI | FE only (TDD-first) | AudiencePanel.test.tsx (3 test) → DocumentPage.tsx + AudiencePanel.tsx + useAudience.ts | 3 test TDD (red→green) + typecheck + build + 43+3=46 FE + 67 BE + E2E | Thấp |
| 2 | Ops go-live | Infra | nginx.conf, docker-compose.prod, DEPLOY.md | nginx -t, compose config, ef script, E2E prod | CAO (prod) |
| 3 | /classify lại | Meta | project_state.yaml | git ls-files wc -l | Thấp |
| 4 | RAG/BOT S18 | New service | apps/rag/ (Python), apps/web/src/features/bot/ | pytest + E2E + BR-06 | CAO (new dep) |
| 5 | Redis cache REC | BE | RecommendationService.cs, Program.cs, appsettings.json | dotnet test + Redis CLI | Thấp-Trung |
| 6 | Access event tracking | BE + DB | Migration + DocEvent + ReportService + DocumentService | dotnet test + E2E + privacy | Trung |

> **Khuyến nghị:** Làm tuần tự #0 → #1 → #3 trước (thấp rủi ro, nhanh). #0 là meta (điền template, không đổi code). #1 là TDD-first (viết 3 test red trước, implement green). #2 cần user approve (prod). #4-#6 cần spec riêng + user approve (Hard Stop: install dependency, schema migration).

