#!/bin/bash
# Tạo role + DB riêng cho Keycloak (prod). Postgres image chỉ chạy /docker-entrypoint-initdb.d
# KHI VOLUME TRỐNG (lần đầu). Volume đã có dữ liệu → chạy tay SQL tương đương (deploy/DEPLOY.md §5).
set -euo pipefail

: "${KC_DB_PASSWORD:?set KC_DB_PASSWORD}"
KC_DB_NAME="${KC_DB_NAME:-keycloak}"
KC_DB_USERNAME="${KC_DB_USERNAME:-keycloak}"

# Biến psql (:"ident" / :'literal') tự quote → không ghép chuỗi SQL thô.
psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" \
  -v kc_db="$KC_DB_NAME" -v kc_user="$KC_DB_USERNAME" -v kc_pass="$KC_DB_PASSWORD" <<'SQL'
CREATE ROLE :"kc_user" LOGIN PASSWORD :'kc_pass';
CREATE DATABASE :"kc_db" OWNER :"kc_user";
SQL
