# QGP — Quản lý secret (A5, ADR-0012)

Nguyên tắc: **không commit secret vào Git/log**. 3 tầng theo ADR-0012:

| Môi trường | Nguồn secret |
|---|---|
| **Dev** | .NET Secret Manager (`dotnet user-secrets`) hoặc env var cục bộ |
| **CI (GitLab)** | GitLab **masked + protected** variables |
| **Prod (hiện tại)** | **Docker secrets** — file trên host `deploy/secrets/` → `/run/secrets/<KEY>` |
| **Prod (sau này)** | **HashiCorp Vault** (ưu tiên dynamic DB creds) → Vault Agent ghi ra cùng tên file |

---

## Cách BE nạp secret

Thứ tự ưu tiên config (sau ghi đè trước): appsettings → env var → **file secret**.

BE đọc secret dạng **file** qua `AddKeyPerFile` (Program.cs):
- Thư mục: `QGP_SECRETS_DIR` (mặc định `/run/secrets`).
- **Tên file = config key**, dùng `__` cho lồng nhau. Nội dung file = giá trị secret (1 dòng, newline cuối được bỏ).
- DB connection + Meili key đọc qua `QgpSecrets` (`Infrastructure/QgpSecrets.cs`) — **file > env > appsettings**.
  ⚠️ Không đọc secret bằng `Environment.GetEnvironmentVariable` — sẽ bỏ qua file secret (lỗi đã sửa 2026-10-01).

Ví dụ file:
```
/run/secrets/QGP_DB_CONNECTION          → ConnectionStrings/QGP_DB_CONNECTION
/run/secrets/QGP_MEILI_KEY              → Meili key
/run/secrets/Auth__ClientSecret         → Auth:ClientSecret (nếu dùng confidential client)
/run/secrets/Cors__AllowedOrigins       → Cors:AllowedOrigins
```

> Cơ chế này hoạt động với **Vault Agent** (render secret ra file) VÀ **Docker/Swarm secrets**
> (mount ở `/run/secrets`) mà KHÔNG cần thêm thư viện.

---

## Dev — .NET Secret Manager

```bash
cd apps/api/src/Qgp.Api
dotnet user-secrets init
dotnet user-secrets set "Cors:AllowedOrigins" "http://localhost:5173"
# secret nằm ngoài repo (~/.microsoft/usersecrets), KHÔNG commit
```

---

## CI — GitLab masked/protected variables

Settings → CI/CD → Variables (bật *Masked* + *Protected*):
`OIDC_AUTHORITY`, `POSTGRES_PASSWORD`, `MEILI_MASTER_KEY`, `KEYCLOAK_ADMIN_PASSWORD`, …
Pipeline dùng qua `$VAR`; KHÔNG in ra log.

---

## Prod — Docker secrets (đang dùng)

`deploy/docker-compose.prod.yml` khai báo 3 secret, nguồn `${QGP_SECRETS_DIR:-./secrets}/<KEY>`
(tương đối với `deploy/`; thư mục `deploy/secrets/` đã gitignore):

| File | Dùng bởi | Ghi chú |
|---|---|---|
| `POSTGRES_PASSWORD` | `postgres` (`POSTGRES_PASSWORD_FILE`) | chỉ áp khi khởi tạo volume lần đầu |
| `QGP_DB_CONNECTION` | `migrate`, `api` | connection string đầy đủ, mật khẩu trùng file trên |
| `QGP_MEILI_KEY` | `api` | trùng `MEILI_MASTER_KEY` trong `.env.prod` |

```bash
install -d -m 700 deploy/secrets
printf '%s' '<giá trị>' > deploy/secrets/QGP_DB_CONNECTION   # tương tự 2 file còn lại
chmod 644 deploy/secrets/*
```

> ⚠️ Compose (không Swarm) mount secret dạng **bind-mount giữ nguyên owner/mode của file trên host**.
> Container chạy non-root (`api` = uid 1654 `app`, `postgres` = user postgres) → file phải đọc được
> (0644). Bảo vệ bằng quyền thư mục `deploy/secrets/` (0700, owner root/deploy user).

**Còn trong `.env.prod` (chưa chuyển được sang file):** `MEILI_MASTER_KEY` (container Meili không có
`*_FILE`), `KC_DB_PASSWORD` + `KEYCLOAK_ADMIN_PASSWORD` (Keycloak đọc env), `GRAFANA_ADMIN_PASSWORD`.
Giữ `.env.prod` mode 600, không commit.

## Prod — Vault Agent (bước tiếp theo, mẫu)

1. Bật Vault + KV/Database secrets engine.
2. Vault Agent template render secret ra `/run/secrets/…` (BE tự nạp qua `AddKeyPerFile`):

```hcl
# vault-agent template ví dụ
template {
  destination = "/run/secrets/QGP_DB_CONNECTION"
  contents    = "Host=postgres;Port=5432;Database=qgp_db;Username={{ with secret \"database/creds/qgp\" }}{{ .Data.username }};Password={{ .Data.password }}{{ end }}"
}
```
3. Mount `/run/secrets` (tmpfs) vào container `api`.
4. Dynamic DB creds: Vault cấp user Postgres tạm, tự rotate → BE nạp lại khi Agent ghi file mới (restart nhẹ hoặc reload).

> Chuyển từ Docker secrets sang Vault: Vault Agent render đúng 3 tên file trên vào một thư mục
> (tmpfs), đặt `QGP_SECRETS_DIR` trỏ tới đó — không cần sửa code hay compose service.
> `deploy/.env.prod` + `deploy/secrets/` đã có trong `.gitignore`.
