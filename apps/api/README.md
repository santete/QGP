# qgp-api — Quality Governance Portal (Backend)

.NET 8 · ASP.NET Core · EF Core + Npgsql · PostgreSQL 16. Tầng dữ liệu QGP: 12 bảng, schema `qgp`, ràng buộc governance BR-01/02/07 enforce tại DB.

## Sprint 1 — T5 (tầng dữ liệu)
- Domain entities 12 bảng (`src/Qgp.Api/Domain/Entities`) + enums (map text).
- `QgpDbContext` (`Infrastructure/Persistence`): schema `qgp`, snake_case, id `UUID DEFAULT gen_random_uuid()`, `created_at/updated_at`, index §2.4.
- Ràng buộc: **`uq_one_effective_per_doc`** (partial unique BR-02), **`ck_effective_ge_issue`** (BR-07), **`uq_doc_id`** (BR-01).
- Seed 6 role RBAC (READER/CONTRIBUTOR/AUTHOR/APPROVER/QA_LEAD/ADMIN — SDD §6.2).
- Migration `InitialCreate` (`Infrastructure/Persistence/Migrations`).

## Lệnh
```bash
dotnet build apps/api/Qgp.Api.sln
dotnet test  apps/api/Qgp.Api.sln          # 7 test model-metadata (không cần DB)

# EF (chạy trong apps/api — nơi có tool-manifest):
cd apps/api
dotnet ef migrations script --idempotent --project src/Qgp.Api --startup-project src/Qgp.Api -o init.sql
dotnet ef database update   --project src/Qgp.Api --startup-project src/Qgp.Api   # cần Postgres (T1 docker-compose)
```

## Connection string
Ưu tiên env `QGP_DB_CONNECTION`, fallback `ConnectionStrings:Qgp` (appsettings). Dev mặc định `Host=localhost;Database=qgp_db;Username=qgp;Password=qgp`. Prod: secret qua Vault (SDD §1.5), KHÔNG commit.

## Chưa làm (bước sau)
- `dotnet ef database update` cần Postgres đang chạy → **T1/E0.1 docker-compose** (postgres/meili/redis).
- Partition `audit_logs` theo tháng (SDD §2.4) → migration raw-SQL bổ sung.
- Wire controllers từ `product-spec/generated/qgp-api-server` + services (E1.x).
- OTel/Serilog/FluentValidation/Polly/Quartz.NET (E0.6).

## Governance
Naming bám `docs/ai/internal_rules/02_Naming_Microservice.md` (R-SQL): schema `qgp` (không `public`), table snake_case số nhiều, `id UUID`, `created_at/updated_at`. Enum/field khớp `product-spec/api/openapi.yaml`.
