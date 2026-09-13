# QGP — Quản lý secret (A5, ADR-0012)

Nguyên tắc: **không commit secret vào Git/log**. 3 tầng theo ADR-0012:

| Môi trường | Nguồn secret |
|---|---|
| **Dev** | .NET Secret Manager (`dotnet user-secrets`) hoặc env var cục bộ |
| **CI (GitLab)** | GitLab **masked + protected** variables |
| **Prod** | **HashiCorp Vault** (ưu tiên dynamic DB creds) → Vault Agent ghi ra file |

---

## Cách BE nạp secret

Thứ tự ưu tiên config (sau ghi đè trước): appsettings → env var → **file secret**.

BE đọc secret dạng **file** qua `AddKeyPerFile` (Program.cs):
- Thư mục: `QGP_SECRETS_DIR` (mặc định `/run/secrets`).
- **Tên file = config key**, dùng `__` cho lồng nhau. Nội dung file = giá trị secret (1 dòng).

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

## Prod — Vault Agent (mẫu)

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

> ⚠️ `deploy/.env.prod` chỉ dùng cho staging/bootstrap. Prod thật: chuyển các giá trị nhạy cảm
> sang Vault, để `.env.prod` chỉ còn tham chiếu (hoặc bỏ hẳn), thêm `deploy/.env.prod` vào `.gitignore`.
