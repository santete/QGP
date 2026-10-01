# QGP — Đóng gói & Triển khai (A3+A4, ADR-0011)

Stack: Docker + docker-compose + Nginx (+TLS) trên Ubuntu. GitLab CI: validate → test → package.

---

## 1. Ảnh Docker

| Ảnh | Context | Nội dung |
|---|---|---|
| `qgp-api` | `apps/api` | .NET 8, multi-stage, chạy non-root (`app`), lắng `:8080`, content-repo ở volume `/data/content-repo` |
| `qgp-web` | `apps/web` | Vite build → Nginx serve SPA + reverse-proxy `/v1`,`/auth`,`/healthz` → service `api` |
| `qgp-api` target `migrator` | `apps/api` | EF migration bundle — chạy một lần trước `api` (service `migrate`) |

Build thử cục bộ:
```bash
docker build -t qgp-api apps/api
docker build -t qgp-web --build-arg VITE_OIDC_AUTHORITY=http://localhost:8081/realms/qgp apps/web
```

> ⚠️ FE bake biến `VITE_*` **lúc build**. Đổi authority/domain ⇒ phải build lại `qgp-web`.

---

## 2. Chạy full stack (staging/prod)

```bash
cp deploy/.env.prod.example deploy/.env.prod      # rồi điền giá trị thật
# Docker secrets (deploy/SECRETS.md): 3 file, mỗi file 1 giá trị
install -d -m 700 deploy/secrets
printf '%s' 'mat-khau-postgres' > deploy/secrets/POSTGRES_PASSWORD
printf '%s' 'Host=postgres;Port=5432;Database=qgp_db;Username=qgp;Password=mat-khau-postgres' > deploy/secrets/QGP_DB_CONNECTION
printf '%s' 'meili-master-key-trung-MEILI_MASTER_KEY' > deploy/secrets/QGP_MEILI_KEY
chmod 644 deploy/secrets/*                          # container chạy non-root phải đọc được
docker compose -f deploy/docker-compose.prod.yml --env-file deploy/.env.prod up -d --build
```

Thành phần: `postgres` · `meilisearch` · `redis` · `keycloak` · `migrate` (chạy một lần) · `api` · `web`.
Điểm vào công khai duy nhất = `web` (Nginx) cổng 80/443. Keycloak mở riêng cổng `${KEYCLOAK_PORT}`
(trình duyệt gọi thẳng IdP để login — không đi qua Nginx).

**Migration DB tự động:** API KHÔNG tự migrate ở Production. Service `migrate` (image target `migrator`,
EF bundle) chạy `efbundle` một lần rồi thoát; `api` chỉ start khi `migrate` exit 0
(`service_completed_successfully`). Bundle idempotent — chạy lại khi không có migration mới là no-op.
Connection đọc từ secret `/run/secrets/QGP_DB_CONNECTION` (hoặc env `QGP_DB_CONNECTION`).

```bash
docker compose -f deploy/docker-compose.prod.yml --env-file deploy/.env.prod logs migrate   # xem kết quả
docker compose -f deploy/docker-compose.prod.yml --env-file deploy/.env.prod run --rm migrate  # chạy lại tay
```

> Trước khi deploy bản có migration mới: review SQL bằng
> `dotnet ef migrations script --idempotent --project apps/api/src/Qgp.Api` (không có DROP/ALTER phá data),
> và backup DB (§9).

**Kiểm chứng cục bộ trước go-live** (không chạm prod, container tạm `qgp-verify-*`):
```bash
deploy/verify-golive.sh            # all: migrator · secrets · keycloak · csp · observability
deploy/verify-golive.sh csp        # từng phần
```

---

## 3. Biến môi trường then chốt (xem `.env.prod.example`)

| Biến | Ý nghĩa |
|---|---|
| `deploy/secrets/*` | secret DB / Meili (Docker secrets, không phải env) — `SECRETS.md` |
| `MEILI_MASTER_KEY` | key cho container Meili (không hỗ trợ `*_FILE`) — trùng `secrets/QGP_MEILI_KEY` |
| `OIDC_AUTHORITY` | URL realm Keycloak **trình duyệt** truy cập được (prod: https) |
| `OIDC_REQUIRE_HTTPS` | `true` ở prod (https), `false` nếu Keycloak chạy http nội bộ |
| `CORS_ALLOWED_ORIGINS` | origin FE thật, csv nếu nhiều — thay hardcode `localhost:5173` |
| `KEYCLOAK_ADMIN_PASSWORD` | mật khẩu admin Keycloak (bootstrap) |
| `KC_HOSTNAME` | URL công khai Keycloak; cũng là `connect-src` của CSP |
| `KC_DB_PASSWORD` | mật khẩu DB `keycloak` |
| `QGP_WEB_ORIGIN` | origin FE → redirect URI client `qgp-web` (realm prod) |
| `CSP_HEADER` | `Content-Security-Policy-Report-Only` (mặc định) / `Content-Security-Policy` |

---

## 4. TLS (Nginx)

Mặc định `apps/web/nginx/default.conf.template` chạy HTTP (đặt sau LB hoặc dev). Bật HTTPS tại Nginx:
1. Bỏ comment block `server { listen 443 ssl; ... }` trong template + dòng redirect 80→443.
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

## 5. Keycloak prod

Compose prod chạy `start --import-realm` (không `-dev`):
- **DB:** Postgres chung instance, DB + role riêng `keycloak` (`KC_DB=postgres`). Tạo bởi
  `deploy/postgres/init/10-keycloak-db.sh` — **chỉ chạy khi volume Postgres còn trống**.
  Volume đã có dữ liệu → tạo tay một lần:
  ```bash
  # biến psql chỉ được thay khi SQL đi qua stdin (KHÔNG thay trong -c)
  docker compose -f deploy/docker-compose.prod.yml --env-file deploy/.env.prod exec -T postgres \
    psql -v ON_ERROR_STOP=1 -U qgp -d qgp_db -v kc_pass='<KC_DB_PASSWORD>' <<'SQL'
  CREATE ROLE keycloak LOGIN PASSWORD :'kc_pass';
  CREATE DATABASE keycloak OWNER keycloak;
  SQL
  ```
- **Hostname/TLS:** `KC_HOSTNAME` = URL công khai (https). TLS terminate ở reverse proxy/LB phía trước,
  Keycloak tin header `X-Forwarded-*` (`KC_PROXY_HEADERS=xforwarded`). Proxy PHẢI ghi đè các header này.
- **Realm:** `deploy/keycloak/realm-qgp.prod.json` — roles + clients `qgp-api`/`qgp-web`, **không user mẫu**,
  `sslRequired=external`, tắt password grant (`directAccessGrantsEnabled=false`). `${QGP_WEB_ORIGIN}` được
  Keycloak thay từ env lúc import. Import **bỏ qua nếu realm `qgp` đã tồn tại** → đổi cấu hình realm sau đó
  qua Admin Console/`kcadm.sh`, không sửa file. `realm-qgp.json` (user mẫu `Password123!`) **chỉ dùng dev**.
- **User:** tạo qua Admin Console hoặc federation (LDAP/AD); gán realm role QGP (`reader`…`admin`).
- **Health:** `KC_HEALTH_ENABLED=true` → `/health/ready` trên management port 9000 (không publish).

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
  Nginx (SPA) gắn 3 header đó + **CSP** từ `apps/web/nginx/security-headers.inc.template` (cả `/assets/`).
- **CSP**: render lúc container start từ env (không build lại khi đổi domain):
  `CSP_CONNECT_SRC` = origin Keycloak (compose lấy `KC_HOSTNAME`), `CSP_HEADER` mặc định
  `Content-Security-Policy-Report-Only` — chỉ báo vi phạm ở console trình duyệt, không chặn.
  Quy trình: go-live với Report-Only → đăng nhập SSO + đi các màn chính, xem console không có
  `[Report Only]` → đặt `CSP_HEADER=Content-Security-Policy` trong `.env.prod` → `up -d web`.
  Policy cho phép Google Fonts (`src/theme/tokens.css` @import Inter).
- **HTTPS-redirect + HSTS**: bật block `443` trong `nginx.conf` (mục 4).

## 8. Secrets (A5, ADR-0012)

Xem **`deploy/SECRETS.md`**. Tóm tắt: dev = .NET Secret Manager · CI = GitLab masked vars ·
prod = **Docker secrets** (`deploy/secrets/*` → `/run/secrets/<KEY>`) → BE nạp qua `AddKeyPerFile`
(`QgpSecrets`: file > env > appsettings). Lên Vault sau: Vault Agent render ra cùng tên file, không sửa code.

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
stack riêng **`deploy/observability/docker-compose.yml`** (project `qgp-obs`), chạy SAU app stack:

```bash
docker compose -f deploy/observability/docker-compose.yml --env-file deploy/.env.prod up -d
ssh -L 3000:127.0.0.1:3000 <host>    # Grafana chỉ bind loopback → mở http://localhost:3000
```

| Thành phần | Image | Vai trò |
|---|---|---|
| `otel-collector` | `otel/opentelemetry-collector-contrib:0.111.0` | nhận OTLP :4317/:4318, gắn vào network app (`QGP_APP_NETWORK`) |
| `prometheus` | `prom/prometheus:v2.54.1` | scrape collector :8889, retention `PROMETHEUS_RETENTION` (15d) |
| `tempo` | `grafana/tempo:2.6.0` | traces, lưu local 14 ngày |
| `loki` | `grafana/loki:3.2.0` | logs qua OTLP (`/otlp`), 14 ngày |
| `grafana` | `grafana/grafana:11.2.2` | datasource provision sẵn (uid `prometheus`/`tempo`/`loki`) |

Chưa chạy stack này thì để `OTEL_OTLP_ENDPOINT` rỗng (tắt export) — tránh api export tới host không tồn tại. Envelope lỗi ADR-0014 GIỮ NGUYÊN (đã verify: ISC log
exception kèm TraceId, `QgpExceptionHandler` vẫn trả `{error:{code,message}}`).
