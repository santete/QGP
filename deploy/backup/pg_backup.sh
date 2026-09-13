#!/usr/bin/env bash
# A6 — Backup Postgres qgp (pg_dump + retention). Chạy trên host Ubuntu (cron) hoặc thủ công.
# Cron ví dụ (2h sáng hằng ngày):  0 2 * * *  /opt/qgp/deploy/backup/pg_backup.sh
set -euo pipefail

BACKUP_DIR="${QGP_BACKUP_DIR:-/var/backups/qgp}"
PG_CONTAINER="${QGP_PG_CONTAINER:-qgp-prod-postgres-1}"
PG_USER="${POSTGRES_USER:-qgp}"
PG_DB="${POSTGRES_DB:-qgp_db}"
RETENTION_DAYS="${QGP_BACKUP_RETENTION_DAYS:-14}"

mkdir -p "$BACKUP_DIR"
STAMP="$(date +%Y%m%d_%H%M%S)"
OUT="$BACKUP_DIR/qgp_${STAMP}.sql.gz"

echo "[pg_backup] dump $PG_DB (container $PG_CONTAINER) -> $OUT"
docker exec "$PG_CONTAINER" pg_dump -U "$PG_USER" -d "$PG_DB" --no-owner | gzip > "$OUT"

echo "[pg_backup] xoa backup cu hon ${RETENTION_DAYS} ngay"
find "$BACKUP_DIR" -name 'qgp_*.sql.gz' -type f -mtime +"$RETENTION_DAYS" -delete

echo "[pg_backup] done ($(du -h "$OUT" | cut -f1))"
# Khôi phục:  gunzip -c qgp_YYYYmmdd_HHMMSS.sql.gz | docker exec -i <pg> psql -U qgp -d qgp_db
