#!/usr/bin/env bash
# Kiểm chứng go-live CỤC BỘ (không chạm prod). Mọi container/network tạm có prefix 'qgp-verify-'
# và được dọn khi thoát. Chạy từ bất kỳ đâu:
#   deploy/verify-golive.sh [migrator|secrets|keycloak|csp|observability|all]   (mặc định: all)
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
NET=qgp-verify-net
PG=qgp-verify-pg
TMP="$(mktemp -d)"

cleanup() {
  docker rm -f $(docker ps -aq --filter "name=qgp-verify-") >/dev/null 2>&1 || true
  docker network rm "$NET" >/dev/null 2>&1 || true
  rm -rf "$TMP"
}
trap cleanup EXIT

pass() { echo "  ✅ $*"; }
fail() { echo "  ❌ $*" >&2; exit 1; }

start_pg() {
  docker network inspect "$NET" >/dev/null 2>&1 || docker network create "$NET" >/dev/null
  docker run -d --name "$PG" --network "$NET" -e POSTGRES_USER=qgp -e POSTGRES_PASSWORD=verify \
    -e POSTGRES_DB=qgp_db "$@" pgvector/pgvector:pg16 >/dev/null
  # pg_isready đã OK trong lúc server TẠM của initdb chạy → chờ init xong rồi mới check server thật.
  for _ in $(seq 1 60); do
    if docker logs "$PG" 2>&1 | grep -q "PostgreSQL init process complete" \
      && docker exec "$PG" pg_isready -U qgp -d qgp_db >/dev/null 2>&1; then
      return 0
    fi
    sleep 1
  done
  fail "postgres tạm không sẵn sàng"
}

# ── #2 Migration khi deploy: image target 'migrator' (EF bundle), idempotent, đọc conn từ env HOẶC file secret.
check_migrator() {
  echo "▶ migrator"
  docker build -q --target migrator -t qgp-verify-migrator "$ROOT/apps/api" >/dev/null \
    || fail "build target migrator lỗi"
  start_pg
  local conn="Host=$PG;Port=5432;Database=qgp_db;Username=qgp;Password=verify"

  docker run --rm --name qgp-verify-mig1 --network "$NET" -e QGP_DB_CONNECTION="$conn" qgp-verify-migrator >/dev/null \
    || fail "lần 1 (env) lỗi"
  # Lần 2: không env, chỉ file secret → phải đọc /run/secrets/QGP_DB_CONNECTION và no-op (idempotent).
  printf '%s\n' "$conn" > "$TMP/QGP_DB_CONNECTION"
  docker run --rm --name qgp-verify-mig2 --network "$NET" \
    -v "$TMP/QGP_DB_CONNECTION:/run/secrets/QGP_DB_CONNECTION:ro" qgp-verify-migrator >/dev/null \
    || fail "lần 2 (file secret) lỗi"

  local expected applied
  expected=$(find "$ROOT/apps/api/src/Qgp.Api/Infrastructure/Persistence/Migrations" -name '*.Designer.cs' | wc -l)
  applied=$(docker exec "$PG" psql -U qgp -d qgp_db -tAc 'select count(*) from qgp.__ef_migrations_history')
  [ "$applied" -eq "$expected" ] || fail "migration áp $applied/$expected"
  pass "migrator idempotent, env + file secret OK ($applied/$expected migration)"
  cleanup; TMP="$(mktemp -d)"
}

# Secret giả cho compose config (deploy/secrets/ thật KHÔNG commit).
fake_secrets() {
  mkdir -p "$TMP/secrets"
  for k in QGP_DB_CONNECTION QGP_MEILI_KEY POSTGRES_PASSWORD; do echo "fake-$k" > "$TMP/secrets/$k"; done
}

compose_json() {
  QGP_SECRETS_DIR="$TMP/secrets" docker compose -f "$ROOT/deploy/docker-compose.prod.yml" \
    --env-file "$ROOT/deploy/.env.prod.example" config --format json
}

# ── #3 Docker secrets: secret DB/Meili đi qua /run/secrets, không nằm trong env của container.
check_secrets() {
  echo "▶ secrets"
  fake_secrets
  compose_json > "$TMP/compose.json" || fail "compose config lỗi"
  python3 - "$TMP/compose.json" <<'PY' || fail "compose secrets chưa đúng"
import json, sys
s = json.load(open(sys.argv[1]))["services"]
def targets(svc): return {x.get("target") or x["source"] for x in s[svc].get("secrets", [])}
errs = []
for svc in ("api", "migrate"):
    env = s[svc].get("environment") or {}
    leaked = [k for k in ("QGP_DB_CONNECTION", "QGP_MEILI_KEY") if k in env]
    if leaked: errs.append(f"{svc} còn env {leaked}")
if not {"QGP_DB_CONNECTION", "QGP_MEILI_KEY"} <= targets("api"): errs.append("api thiếu secret")
if "QGP_DB_CONNECTION" not in targets("migrate"): errs.append("migrate thiếu secret")
pg = s["postgres"].get("environment") or {}
if "POSTGRES_PASSWORD" in pg or pg.get("POSTGRES_PASSWORD_FILE") != "/run/secrets/POSTGRES_PASSWORD":
    errs.append("postgres phải dùng POSTGRES_PASSWORD_FILE")
if errs: print("\n".join(errs), file=sys.stderr); sys.exit(1)
PY
  pass "compose: api/migrate/postgres đọc secret từ /run/secrets"

  # Chạy thật image api (Production) CHỈ với file secret → /healthz phải thấy Postgres.
  docker build -q --target migrator -t qgp-verify-migrator "$ROOT/apps/api" >/dev/null
  docker build -q -t qgp-verify-api "$ROOT/apps/api" >/dev/null || fail "build image api lỗi"
  start_pg
  printf 'Host=%s;Port=5432;Database=qgp_db;Username=qgp;Password=verify\n' "$PG" > "$TMP/conn"
  docker run --rm --network "$NET" -v "$TMP/conn:/run/secrets/QGP_DB_CONNECTION:ro" qgp-verify-migrator >/dev/null
  docker run -d --name qgp-verify-api --network "$NET" -p 127.0.0.1::8080 \
    -v "$TMP/conn:/run/secrets/QGP_DB_CONNECTION:ro" \
    -e Auth__Mode=Oidc -e Auth__OidcAuthority=http://sso.invalid/realms/qgp -e Auth__RequireHttpsMetadata=false \
    -e Cors__AllowedOrigins=http://localhost qgp-verify-api >/dev/null
  local port body=""
  port=$(docker port qgp-verify-api 8080/tcp | head -1 | sed 's/.*://')
  for _ in $(seq 1 30); do
    body=$(curl -s "http://127.0.0.1:$port/healthz" || true)
    [ "$body" = "Healthy" ] && break
    sleep 1
  done
  [ "$body" = "Healthy" ] || { docker logs --tail 30 qgp-verify-api >&2; fail "api /healthz='$body' (chỉ có file secret)"; }
  pass "api Production đọc QGP_DB_CONNECTION từ file secret → /healthz Healthy"
  cleanup; TMP="$(mktemp -d)"
}

# ── #4 Keycloak prod: 'start' (không dev) + DB Postgres riêng + realm prod (không user mẫu).
check_keycloak() {
  echo "▶ keycloak"
  fake_secrets
  compose_json > "$TMP/compose.json" || fail "compose config lỗi"
  python3 - "$TMP/compose.json" <<'PY' || fail "compose keycloak chưa đúng"
import json, sys
s = json.load(open(sys.argv[1]))["services"]
kc = s["keycloak"]; env = kc.get("environment") or {}
cmd = kc.get("command") or []
errs = []
if "start-dev" in cmd or "start" not in cmd: errs.append(f"command={cmd} (cần 'start')")
for k, v in {"KC_DB": "postgres", "KC_PROXY_HEADERS": "xforwarded"}.items():
    if env.get(k) != v: errs.append(f"{k}={env.get(k)}")
for k in ("KC_HOSTNAME", "KC_DB_URL", "KC_DB_PASSWORD"):
    if not env.get(k): errs.append(f"thiếu {k}")
mounts = [v.get("source", "") for v in kc.get("volumes", [])]
if any(m.endswith("realm-qgp.json") for m in mounts): errs.append("vẫn mount realm DEV (user mẫu)")
if "postgres" not in (kc.get("depends_on") or {}): errs.append("thiếu depends_on postgres")
if errs: print("\n".join(errs), file=sys.stderr); sys.exit(1)
PY
  pass "compose: keycloak start + Postgres + hostname/proxy, không mount realm dev"

  [ -f "$ROOT/deploy/postgres/init/10-keycloak-db.sh" ] || fail "thiếu deploy/postgres/init/10-keycloak-db.sh"
  [ -f "$ROOT/deploy/keycloak/realm-qgp.prod.json" ] || fail "thiếu deploy/keycloak/realm-qgp.prod.json"
  start_pg -e KC_DB_NAME=keycloak -e KC_DB_USERNAME=keycloak -e KC_DB_PASSWORD=kc-verify \
    -v "$ROOT/deploy/postgres/init:/docker-entrypoint-initdb.d:ro"
  docker exec "$PG" psql -U qgp -d qgp_db -tAc "select 1 from pg_database where datname='keycloak'" | grep -q 1 \
    || fail "init script không tạo DB keycloak"

  local origin="https://qgp.verify.local"
  docker run -d --name qgp-verify-kc --network "$NET" -p 127.0.0.1::8080 -p 127.0.0.1::9000 \
    -e KC_BOOTSTRAP_ADMIN_USERNAME=admin -e KC_BOOTSTRAP_ADMIN_PASSWORD=admin-verify \
    -e KC_DB=postgres -e KC_DB_URL="jdbc:postgresql://$PG:5432/keycloak" \
    -e KC_DB_USERNAME=keycloak -e KC_DB_PASSWORD=kc-verify \
    -e KC_HOSTNAME=http://localhost:8080 -e KC_PROXY_HEADERS=xforwarded \
    -e KC_HTTP_ENABLED=true -e KC_HEALTH_ENABLED=true -e QGP_WEB_ORIGIN="$origin" \
    -v "$ROOT/deploy/keycloak/realm-qgp.prod.json:/opt/keycloak/data/import/realm-qgp.json:ro" \
    quay.io/keycloak/keycloak:26.0 start --import-realm >/dev/null
  local http mgmt ready=""
  http=$(docker port qgp-verify-kc 8080/tcp | head -1 | sed 's/.*://')
  mgmt=$(docker port qgp-verify-kc 9000/tcp | head -1 | sed 's/.*://')
  for _ in $(seq 1 120); do
    ready=$(curl -s -o /dev/null -w '%{http_code}' "http://127.0.0.1:$mgmt/health/ready" || true)
    [ "$ready" = "200" ] && break
    sleep 2
  done
  [ "$ready" = "200" ] || { docker logs --tail 40 qgp-verify-kc >&2; fail "keycloak /health/ready=$ready"; }
  pass "keycloak start + Postgres: /health/ready 200"

  local token
  token=$(curl -s "http://127.0.0.1:$http/realms/master/protocol/openid-connect/token" \
    -d grant_type=password -d client_id=admin-cli -d username=admin -d password=admin-verify \
    | python3 -c 'import json,sys; print(json.load(sys.stdin)["access_token"])') || fail "không lấy được admin token"
  local users redirect
  users=$(curl -s -H "Authorization: Bearer $token" "http://127.0.0.1:$http/admin/realms/qgp/users/count")
  redirect=$(curl -s -H "Authorization: Bearer $token" "http://127.0.0.1:$http/admin/realms/qgp/clients?clientId=qgp-web" \
    | python3 -c 'import json,sys; c=json.load(sys.stdin)[0]; print(",".join(c["redirectUris"])+"|"+str(c["directAccessGrantsEnabled"]))')
  [ "$users" = "0" ] || fail "realm qgp có $users user (prod phải 0)"
  [ "$redirect" = "$origin/*|False" ] || fail "qgp-web redirect/grants='$redirect' (mong '$origin/*|False')"
  pass "realm prod: 0 user, redirect từ QGP_WEB_ORIGIN, tắt password grant"
  cleanup; TMP="$(mktemp -d)"
}

# ── #5 CSP nginx: template env lúc container start, Report-Only mặc định, /assets giữ security header.
run_web() {  # $1=tên container, phần còn lại = docker run args
  local name=$1; shift
  # nginx resolve upstream 'api' lúc start → map tạm về loopback.
  docker run -d --name "$name" --add-host api:127.0.0.1 -p 127.0.0.1::80 "$@" qgp-verify-web >/dev/null
  local port
  port=$(docker port "$name" 80/tcp | head -1 | sed 's/.*://')
  for _ in $(seq 1 20); do curl -s -o /dev/null "http://127.0.0.1:$port/" && break; sleep 0.5; done
  echo "$port"
}

check_csp() {
  echo "▶ csp"
  docker build -q -t qgp-verify-web --build-arg VITE_OIDC_AUTHORITY=https://sso.verify.local/realms/qgp \
    "$ROOT/apps/web" >/dev/null || fail "build image web lỗi"
  local origin="https://sso.verify.local" port h asset
  port=$(run_web qgp-verify-web1 -e CSP_CONNECT_SRC="$origin")
  docker exec qgp-verify-web1 nginx -t >/dev/null 2>&1 || fail "nginx -t lỗi"
  h=$(curl -sI "http://127.0.0.1:$port/")
  echo "$h" | grep -i '^content-security-policy-report-only:' | grep -q "connect-src 'self' $origin" \
    || fail "/ thiếu CSP Report-Only với connect-src $origin"
  echo "$h" | grep -i '^content-security-policy-report-only:' | grep -q 'fonts.googleapis.com' \
    || fail "CSP chưa cho Google Fonts (tokens.css @import)"
  echo "$h" | grep -qi '^content-security-policy:' && fail "mặc định không được enforce"
  asset=$(docker exec qgp-verify-web1 sh -c 'ls /usr/share/nginx/html/assets | head -1')
  h=$(curl -sI "http://127.0.0.1:$port/assets/$asset")
  echo "$h" | grep -qi '^x-frame-options: DENY' || fail "/assets/ mất X-Frame-Options (add_header ở location ghi đè)"
  echo "$h" | grep -qi '^cache-control:.*immutable' || fail "/assets/ mất Cache-Control immutable"
  pass "CSP Report-Only + origin từ env, Google Fonts, /assets giữ header + cache"

  port=$(run_web qgp-verify-web2 -e CSP_CONNECT_SRC="$origin" -e CSP_HEADER=Content-Security-Policy)
  curl -sI "http://127.0.0.1:$port/" | grep -i '^content-security-policy:' | grep -q "$origin" \
    || fail "CSP_HEADER=Content-Security-Policy không enforce"
  pass "CSP_HEADER=Content-Security-Policy → enforce"
  cleanup; TMP="$(mktemp -d)"
}

# ── #6 Observability: stack riêng (collector/Prometheus/Tempo/Loki/Grafana) nối network app;
#    E2E: image api thật gửi OTLP → trace 'qgp-api' xuất hiện trong Tempo.
OBS=(docker compose -p qgp-verify-obs -f "$ROOT/deploy/observability/docker-compose.yml")

obs_down() { "${OBS[@]}" down -v >/dev/null 2>&1 || true; }

gf() {  # GET Grafana API (admin) — $1 = path
  curl -s -u "admin:verify" "http://127.0.0.1:$GF_PORT$1"
}

check_observability() {
  echo "▶ observability"
  [ -f "$ROOT/deploy/observability/docker-compose.yml" ] || fail "thiếu deploy/observability/docker-compose.yml"
  export QGP_APP_NETWORK=$NET GRAFANA_ADMIN_PASSWORD=verify GRAFANA_PORT=0   # GRAFANA_PORT=0 → port ngẫu nhiên
  trap 'obs_down; cleanup' EXIT
  start_pg
  "${OBS[@]}" up -d --quiet-pull >/dev/null 2>&1 || { "${OBS[@]}" up -d; fail "compose up lỗi"; }
  GF_PORT=$("${OBS[@]}" port grafana 3000 | sed 's/.*://')
  for _ in $(seq 1 60); do [ "$(gf /api/health | grep -c '"database": *"ok"')" = 1 ] && break; sleep 1; done

  local uid st
  # Plugin Tempo (Grafana 11.2) không có /health → ping Tempo qua proxy datasource.
  st=""
  for _ in $(seq 1 60); do
    st=$(gf /api/datasources/proxy/uid/tempo/api/echo || true)
    [ "$st" = "echo" ] && break
    sleep 2
  done
  [ "$st" = "echo" ] || { "${OBS[@]}" logs --tail 20 tempo >&2 || true; fail "Tempo qua Grafana proxy='$st'"; }
  for uid in prometheus loki; do
    st=""
    for _ in $(seq 1 60); do
      st=$(gf "/api/datasources/uid/$uid/health" | python3 -c 'import json,sys; print(json.load(sys.stdin).get("status",""))' 2>/dev/null || true)
      [ "$st" = "OK" ] && break
      sleep 2
    done
    [ "$st" = "OK" ] || { "${OBS[@]}" logs --tail 20 "$uid" >&2 || true; fail "datasource $uid health=$st"; }
  done
  pass "Grafana: datasource prometheus/tempo/loki OK"

  local up=""
  for _ in $(seq 1 30); do
    up=$(gf '/api/datasources/proxy/uid/prometheus/api/v1/query?query=up%7Bjob%3D%22otel-collector%22%7D' \
      | python3 -c 'import json,sys; r=json.load(sys.stdin)["data"]["result"]; print(r[0]["value"][1] if r else "")' 2>/dev/null || true)
    [ "$up" = "1" ] && break
    sleep 2
  done
  [ "$up" = "1" ] || fail "Prometheus chưa scrape được otel-collector (up=$up)"
  pass "Prometheus scrape otel-collector up=1"

  docker build -q --target migrator -t qgp-verify-migrator "$ROOT/apps/api" >/dev/null
  docker build -q -t qgp-verify-api "$ROOT/apps/api" >/dev/null
  local conn="Host=$PG;Port=5432;Database=qgp_db;Username=qgp;Password=verify"
  docker run --rm --network "$NET" -e QGP_DB_CONNECTION="$conn" qgp-verify-migrator >/dev/null
  docker run -d --name qgp-verify-api --network "$NET" -p 127.0.0.1::8080 -e QGP_DB_CONNECTION="$conn" \
    -e Auth__Mode=Oidc -e Auth__OidcAuthority=http://sso.invalid/realms/qgp -e Auth__RequireHttpsMetadata=false \
    -e Cors__AllowedOrigins=http://localhost -e Otel__OtlpEndpoint=http://otel-collector:4317 qgp-verify-api >/dev/null
  local port found=""
  port=$(docker port qgp-verify-api 8080/tcp | head -1 | sed 's/.*://')
  for _ in $(seq 1 60); do
    curl -s -o /dev/null "http://127.0.0.1:$port/" || true
    curl -s -o /dev/null "http://127.0.0.1:$port/v1/documents" || true
    found=$(gf '/api/datasources/proxy/uid/tempo/api/search?tags=service.name%3Dqgp-api&limit=5' \
      | python3 -c 'import json,sys; print(len(json.load(sys.stdin).get("traces") or []))' 2>/dev/null || true)
    [ -n "$found" ] && [ "$found" -gt 0 ] && break
    sleep 2
  done
  [ -n "$found" ] && [ "$found" -gt 0 ] || { docker logs --tail 20 qgp-verify-api >&2; fail "Tempo không có trace qgp-api"; }
  pass "E2E: api → otel-collector → Tempo ($found trace qgp-api)"
  obs_down; cleanup; TMP="$(mktemp -d)"; trap cleanup EXIT
}

case "${1:-all}" in
  migrator) check_migrator ;;
  secrets) check_secrets ;;
  keycloak) check_keycloak ;;
  csp) check_csp ;;
  observability) check_observability ;;
  all) check_migrator; check_secrets; check_keycloak; check_csp; check_observability ;;
  *) echo "usage: $0 [migrator|secrets|keycloak|csp|observability|all]" >&2; exit 2 ;;
esac
echo "DONE"
