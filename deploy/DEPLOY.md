# QGP — Đóng gói & Triển khai (A3+A4, ADR-0011)

Stack: Docker + docker-compose + Nginx (+TLS) trên Ubuntu. GitLab CI: validate → test → package.

---

## 1. Ảnh Docker

| Ảnh | Context | Nội dung |
|---|---|---|
| `qgp-api` | `apps/api` | .NET 8, multi-stage, chạy non-root (`app`), lắng `:8080`, content-repo ở volume `/data/content-repo` |
| `qgp-web` | `apps/web` | Vite build → Nginx serve SPA + reverse-proxy `/v1`,`/auth`,`/healthz` → service `api` |

Build thử cục bộ:
```bash
docker build -t qgp-api apps/api
docker build -t qgp-web --build-arg VITE_OIDC_AUTHORITY=http://localhost:8081/realms/qgp apps/web
```

> ⚠️ FE bake biến `VITE_*` **lúc build**. Đổi authority/domain ⇒ phải build lại `qgp-web`.

---

## 2. Chạy full stack (staging/prod)

```bash
cp deploy/.env.prod.example deploy/.env.prod      # rồi điền secret thật
docker compose -f deploy/docker-compose.prod.yml --env-file deploy/.env.prod up -d --build
```

Thành phần: `postgres` · `meilisearch` · `redis` · `keycloak` · `api` · `web`.
Điểm vào công khai duy nhất = `web` (Nginx) cổng 80/443. Keycloak mở riêng cổng `${KEYCLOAK_PORT}`
(trình duyệt gọi thẳng IdP để login — không đi qua Nginx).

Áp migration DB lần đầu (schema qgp): API tự KHÔNG chạy migration ở Production. Chạy tay:
```bash
docker compose -f deploy/docker-compose.prod.yml exec api \
  sh -c "cd /app && DOTNET... "   # hoặc dùng job riêng
# Đơn giản hơn: chạy `dotnet ef database update` từ máy CI trỏ QGP_DB_CONNECTION vào Postgres prod.
```

---

## 3. Biến môi trường then chốt (xem `.env.prod.example`)

| Biến | Ý nghĩa |
|---|---|
| `POSTGRES_PASSWORD`, `MEILI_MASTER_KEY` | secret DB / search |
| `OIDC_AUTHORITY` | URL realm Keycloak **trình duyệt** truy cập được (prod: https) |
| `OIDC_REQUIRE_HTTPS` | `true` ở prod (https), `false` nếu Keycloak chạy http nội bộ |
| `CORS_ALLOWED_ORIGINS` | origin FE thật, csv nếu nhiều — thay hardcode `localhost:5173` |
| `KEYCLOAK_ADMIN_PASSWORD` | mật khẩu admin Keycloak |

---

## 4. TLS (Nginx)

Mặc định `apps/web/nginx.conf` chạy HTTP (đặt sau LB hoặc dev). Bật HTTPS tại Nginx:
1. Bỏ comment block `server { listen 443 ssl; ... }` trong `nginx.conf` + dòng redirect 80→443.
2. Mount cert vào `/etc/nginx/certs` (bỏ comment `volumes` của service `web` trong compose).
3. Cert:
   - **Prod**: Let's Encrypt (certbot) hoặc cert nội bộ → `fullchain.pem` + `privkey.pem`.
   - **Staging/test**: self-signed:
     ```bash
     mkdir -p deploy/certs && openssl req -x509 -newkey rsa:2048 -nodes \
       -keyout deploy/certs/privkey.pem -out deploy/certs/fullchain.pem \
       -days 365 -subj "/CN=qgp.local"
     ```
4. Bật HSTS chỉ khi chắc toàn site đã HTTPS (đã có sẵn header trong block 443).

---

## 5. Keycloak prod hardening (ngoài phạm vi A3+A4, để hạng mục sau)

Compose hiện dùng `start-dev --import-realm` (H2 in-memory, tiện staging). Prod thật cần:
- `start` (không `-dev`) + DB ngoài (`KC_DB=postgres`, `KC_DB_URL`, `KC_DB_USERNAME/PASSWORD`).
- `KC_HOSTNAME` cố định + TLS (hoặc sau reverse proxy với `KC_PROXY_HEADERS=xforwarded`).
- Realm quản lý qua export/import có kiểm soát, KHÔNG dùng user mẫu `Password123!`.

---

## 6. CI (GitLab — `.gitlab-ci.yml`)

- `validate`: lint OpenAPI (Redocly).
- `test`: BE (`dotnet test` + service Postgres/Meili) · FE (`typecheck`+`test`+`build`).
- `package`: build & push `qgp-api`, `qgp-web` lên `$CI_REGISTRY_IMAGE` (chỉ default branch / tag).

Biến CI cần đặt (Settings → CI/CD → Variables): `OIDC_AUTHORITY` (cho build web),
registry mặc định GitLab tự cấp (`CI_REGISTRY*`).

---

## 7. Bảo mật (A7)

- **Rate limiting** (BE, `Program.cs`): global fixed-window theo IP + siết `/auth` (chống brute-force login).
  Cấu hình qua env: `RateLimit__PermitLimit` (100), `RateLimit__WindowSeconds` (10),
  `RateLimit__AuthPermitLimit` (10), `RateLimit__AuthWindowSeconds` (60), `RateLimit__Enabled`.
  Vượt ngưỡng → `429`. IP thật lấy từ `X-Forwarded-For` (đã bật ForwardedHeaders — ingress duy nhất là nginx).
- **Security headers**: BE gắn `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`.
  Nginx (SPA) gắn thêm 3 header đó; **CSP** để sẵn (comment) trong `apps/web/nginx.conf` —
  bật + điền origin Keycloak vào `connect-src` khi go-live (sai origin sẽ chặn login OIDC).
- **HTTPS-redirect + HSTS**: bật block `443` trong `nginx.conf` (mục 4).

## 8. Secrets (A5, ADR-0012)

Xem **`deploy/SECRETS.md`**. Tóm tắt: dev = .NET Secret Manager · CI = GitLab masked vars ·
prod = Vault Agent render ra `/run/secrets` → BE tự nạp (`AddKeyPerFile`, `QGP_SECRETS_DIR`).
Chuyển secret nhạy cảm khỏi `.env.prod` sang Vault khi lên prod thật.

## 9. Backup (A6)

Script ở `deploy/backup/` (chạy bằng cron trên host Ubuntu):
- `pg_backup.sh` — `pg_dump` gzip + retention (mặc định 14 ngày). Env: `QGP_BACKUP_DIR`, `QGP_PG_CONTAINER`.
- `content_repo_backup.sh` — `git bundle` content-repo (+ tùy chọn `git push --mirror` lên GitLab qua `QGP_CONTENT_REMOTE`).

```cron
0 2 * * *   /opt/qgp/deploy/backup/pg_backup.sh
30 2 * * *  /opt/qgp/deploy/backup/content_repo_backup.sh
```

## 10. Observability (A7 / ADR-0013) — ✅ DONE

Wire bằng **`ISC.Observability` 1.2.2** (SDK nội bộ FPT, `sangtt-ftel-isc`) trong `Program.cs`:
`builder.AddStandardObservability("qgp-api")` + `app.UseStandardObservability()` (Serilog JSON logs
+ TraceId, OTLP traces/metrics, auto-instrument EF Core/Quartz/HTTP).

**Bật khi có `Otel:OtlpEndpoint`** (mặc định rỗng → tắt; không bật ở Testing). Prod set env:
```
Otel__OtlpEndpoint=http://otel-collector:4317
```
Các cờ instrumentation (`appsettings.json` → `Otel`): `EnableEntityFramework=true`, `EnableQuartz=true`,
`EnableRedis/Mongo/MassTransit/Grpc=false` (khớp stack QGP).

Đích export = OpenTelemetry Collector → Prometheus (metrics) / Tempo (traces) / Loki (logs) / Grafana —
deploy stack Grafana riêng (ngoài compose app). Envelope lỗi ADR-0014 GIỮ NGUYÊN (đã verify: ISC log
exception kèm TraceId, `QgpExceptionHandler` vẫn trả `{error:{code,message}}`).
