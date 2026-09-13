# Project Map

> "Bản đồ" để AI agent hiểu nhanh kiến trúc project. Đọc file này TRƯỚC khi
> đoán file ở đâu. Cập nhật khi cấu trúc thay đổi đáng kể.

---

## Tech Stack

- **Language**: .NET 8 (C# 12) · TypeScript 5.6
- **Framework (BE)**: ASP.NET Core 8 Minimal API (`apps/api`)
- **Framework (FE)**: React 18 + Vite 5 + Tailwind 3.4 (`apps/web`)
- **Database**: PostgreSQL 16 (+pgvector) — schema `qgp`, snake_case (EFCore.NamingConventions)
- **Search**: Meilisearch (index `documents`, chỉ Effective — BR-06)
- **Cache / Queue**: Redis (chưa wire code — DEFER, decision #13); Quartz.NET (scheduler, WF-03 auto Published→Effective)
- **Git content store**: LibGit2Sharp (bare repo local, COMMIT-ON-PUBLISH, ADR-0015)
- **Observability**: ISC.Observability 1.2.2 (SDK FPT, gated theo `Otel:OtlpEndpoint`)
- **Auth**: Keycloak OIDC (RS256/JWKS) · react-oidc-context (FE) · JwtBearer (BE)
- **Test (BE)**: xUnit + WebApplicationFactory (model-metadata, không cần DB thật)
- **Test (FE)**: Vitest 2.1 + Testing Library 16 (jsdom)
- **Lint / Format**: (FE) `tsc -b --noEmit` typecheck; (BE) `dotnet build` (no lint plugin)
- **Package manager**: NuGet (BE) · npm (FE)

---

## Folder Structure

```
qms-governance-platform/
├── apps/
│   ├── api/                       # BE — .NET 8 ASP.NET Core (Qgp.Api)
│   │   ├── src/Qgp.Api/
│   │   │   ├── Api/                # V1Endpoints.cs (Minimal API routes)
│   │   │   ├── Application/        # 19 service (DocumentService, SearchService, ReportService…)
│   │   │   ├── Auth/               # AuthSetup, KeycloakClaimsTransformation, AuthEndpoints
│   │   │   ├── Contracts/          # Dtos.cs (request/response DTO)
│   │   │   ├── Domain/             # Entities, Enums (Business Rules)
│   │   │   ├── Infrastructure/     # Persistence (QgpDbContext, Migrations), Search, Scheduling, Git
│   │   │   ├── Program.cs          # DI, middleware, pipeline
│   │   │   └── appsettings.json    # Config (Auth, Meili, Scheduler, Otel, RateLimit, Cors)
│   │   └── tests/Qgp.Api.Tests/    # xUnit (WebApplicationFactory, không DB thật)
│   ├── web/                       # FE — React 18 + Vite + TS (Minimal UI, Tailwind)
│   │   ├── src/
│   │   │   ├── api/                # client.ts, mock.ts, types.ts (alias schema.d.ts), schema.d.ts (sinh)
│   │   │   ├── auth/              # AuthContext, roles, guards, oidcConfig, tokenRoles
│   │   │   ├── components/         # Card, Badge, StatusBadge, Toast, AppShell…
│   │   │   ├── features/           # documents, feedback, subscriptions, notifications, reports…
│   │   │   ├── i18n/               # strings.ts (tiếng Việt)
│   │   │   ├── pages/             # HomePage, DocumentPage, AdminPage, ReportsPage…
│   │   │   └── test/              # Vitest (*.test.tsx), setup.ts
│   │   ├── nginx.conf             # Reverse proxy + security headers (prod)
│   │   └── package.json           # scripts: dev, build, test, typecheck, gen:api
│   └── rag/                       # (Phase 3, chưa tạo — ADR-0007 Python service riêng)
├── product-spec/                  # PRD, SDD, ADR, openapi.yaml, BUILD_PLAN, ROADMAP
│   ├── adr/                       # ADR-0001..0015
│   └── api/openapi.yaml           # Single source cho API contract (FE gen:api)
├── docs/ai/                       # Rule cho AI agent (file này, CLAUDE.md, GIT_CONVENTION.md…)
├── .claude/memory/                # L2 memory: project_state.yaml, task_tracker.yaml, schema_snapshot.yaml
├── deploy/                        # docker-compose.yml (local), docker-compose.prod.yml, keycloak/, backup/
└── .gitlab-ci.yml                 # CI: build BE+FE, openapi lint (Redocly)
```

---

## Key Modules / Domains

| Module       | Path                                        | Responsibility                              |
|--------------|---------------------------------------------|---------------------------------------------|
| auth         | `apps/api/src/Qgp.Api/Auth/`                | Keycloak OIDC, dev-login, RBAC policy §10.1 |
| application  | `apps/api/src/Qgp.Api/Application/`         | 19 service: Document, Version, Search, Report, Recommendation, Onboarding, Notification, Subscription, Admin, Feedback, Audit… |
| infrastructure | `apps/api/src/Qgp.Api/Infrastructure/`    | Persistence (QgpDbContext, Migrations), Search (MeiliSearchIndex), Scheduling (Quartz), Git (content store) |
| api          | `apps/api/src/Qgp.Api/Api/V1Endpoints.cs`   | Minimal API routes /v1/* + /auth + /admin/*
| domain       | `apps/api/src/Qgp.Api/Domain/`              | Entities, Enums (Business Rules), value objects |
| contracts    | `apps/api/src/Qgp.Api/Contracts/Dtos.cs`   | Request/response DTO (mirror openapi.yaml) |
| fe-api       | `apps/web/src/api/`                         | client.ts (HTTP), mock.ts (dev), types.ts (alias schema.d.ts) |
| fe-auth      | `apps/web/src/auth/`                        | AuthContext (OIDC/dev), roles, guards |
| fe-features   | `apps/web/src/features/`                    | documents, feedback, subscriptions, notifications, reports, recommendations, onboarding |
| fe-pages     | `apps/web/src/pages/`                       | 16 page (Home, Documents, DocumentPage, Editor, Review, Publish, History, KB, Reports, Admin, Onboarding, StartHere, Notifications, Login, Forbidden…) |

---

## Important Files (đọc khi onboarding)

- `CLAUDE.md`                 — AI Agent Operating Pipeline (6-phase, Hard Stops, WISC gates)
- `apps/api/src/Qgp.Api/Program.cs` — DI, middleware, pipeline, startup
- `apps/api/src/Qgp.Api/Api/V1Endpoints.cs` — toàn bộ route /v1, /auth, /admin
- `apps/api/src/Qgp.Api/appsettings.json` — config (Auth, Meili, Scheduler, Otel, RateLimit, Cors, Git)
- `apps/web/src/router.tsx`   — FE routing
- `apps/web/package.json`     — scripts FE (dev, build, test, typecheck, gen:api)
- `product-spec/api/openapi.yaml` — API contract (single source, FE `npm run gen:api`)
- `deploy/.env.prod.example`  — env vars cần thiết cho prod
- `.claude/memory/project_state.yaml` — L2 memory (pattern, sprint, decisions, gotchas)
- `.claude/memory/schema_snapshot.yaml` — DB schema snapshot (load khi touch API/DB)

---

## Build / Run / Test commands

```bash
# ── Backend (apps/api) ──
cd apps/api && dotnet build                    # build BE
cd apps/api && dotnet test                     # test BE (xUnit, 67 test, không DB thật)
cd apps/api && dotnet test --filter DocAudienceTests  # test riêng (filter)

# ── Frontend (apps/web) ──
cd apps/web && npm install                     # setup (first time)
cd apps/web && npm run dev                     # dev (mock API, VITE_USE_MOCK=1)
cd apps/web && npm run dev:real                # dev (real BE, dev-login)
cd apps/web && npm run dev:oidc                # dev (Keycloak OIDC thật)
cd apps/web && npm run test                    # test FE (Vitest, 43 test)
cd apps/web && npm run test -- AudiencePanel   # test riêng (filter)
cd apps/web && npm run test:watch              # test watch mode
cd apps/web && npm run typecheck               # typecheck (tsc -b --noEmit)
cd apps/web && npm run build                   # build FE (tsc -b && vite build)
cd apps/web && npm run gen:api                 # sinh src/api/schema.d.ts từ openapi.yaml

# ── Deploy (deploy/) ──
docker compose -f deploy/docker-compose.yml up -d          # local (postgres, redis, meili, keycloak)
docker compose -f deploy/docker-compose.prod.yml config    # validate prod YAML
```

---

## Architectural Decisions (gốc của các rule)

- **ADR-0001** docs-as-code: nội dung tài liệu lưu Git, publish = ranh giới bất biến BR-03
- **ADR-0004** bot corpus chỉ index Effective (BR-06)
- **ADR-0006** BE = .NET 8 ASP.NET Core (Minimal API)
- **ADR-0008** PostgreSQL 16 + pgvector (snake_case, schema `qgp`)
- **ADR-0009** Meilisearch (search, chỉ Effective)
- **ADR-0010** FE = React 18 + Vite (Minimal UI, Tailwind)
- **ADR-0012** secrets ưu tiên file `/run/secrets` (Vault Agent-ready, AddKeyPerFile)
- **ADR-0013** OTel observability (ISC.Observability SDK FPT, gated)
- **ADR-0014** lean response envelope (KHÔNG 4-field wrapper; error `{error:{code,message}}`)
- **ADR-0015** Git content store = bare repo local + LibGit2Sharp (COMMIT-ON-PUBLISH)
- **Enum DB**: Status/Type lưu text (HasConversion<string>), 8 trạng thái version
- **Test BE**: model-metadata (offline), không cần Postgres/Redis thật
- **OpenAPI 3.1**: KHÔNG dùng `nullable: true`; mỗi operation phải có ≥1 response 4xx (Redocly lint)
- **FE types**: `npm run gen:api` sinh `schema.d.ts` từ openapi.yaml, types.ts chỉ alias

---

## Environments

| Env        | Branch       | URL                          | Deploy                          |
|------------|--------------|------------------------------|---------------------------------|
| local      | any          | localhost:5173 (FE) / :8080 (BE) | docker-compose.yml           |
| dev:oidc   | any          | localhost:5173 (FE) + Keycloak :8081 | `npm run dev:oidc`       |
| staging    | `develop`    | (chưa có)                    | auto on merge                   |
| production | `main`       | (chưa có)                    | manual + approval (Hard Stop)   |

> BE image Production → KHÔNG auto-migrate (chạy `dotnet ef database update` tay trên prod DB).
> realm-qgp.json chỉ DEV (user/pass mẫu Password123!) — PROD phải tạo realm riêng.

---

## What NOT to touch (without asking)

- `apps/api/src/Qgp.Api/Infrastructure/Persistence/Migrations/` — chỉ tạo migration mới, KHÔNG sửa migration cũ
- `.claude/memory/` — chỉ Phase 5 ghi (project_state, task_tracker, schema_snapshot)
- `product-spec/api/openapi.yaml` — chỉ cập nhật khi API contract đổi (FE gen:api theo sau)
- `deploy/keycloak/realm-qgp.json` — chỉ DEV, KHÔNG import ở prod
- `apps/web/src/api/schema.d.ts` — file sinh (gen:api), KHÔNG sửa tay
- `apps/api/src/Qgp.Api/Qgp.Api.csproj` — thêm NuGet cần user approve (Hard Stop)
- `apps/web/package.json` — thêm npm dep cần user approve (Hard Stop)
- `apps/api/src/Qgp.Api/Infrastructure/Persistence/` — schema migration cần user approve (Hard Stop)
