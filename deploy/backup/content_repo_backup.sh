#!/usr/bin/env bash
# A6 — Backup content-repo (Git bare, ADR-0015): git bundle + tùy chọn push mirror lên GitLab.
# Cron ví dụ:  30 2 * * *  /opt/qgp/deploy/backup/content_repo_backup.sh
set -euo pipefail

BACKUP_DIR="${QGP_BACKUP_DIR:-/var/backups/qgp}"
API_CONTAINER="${QGP_API_CONTAINER:-qgp-prod-api-1}"
REPO_IN_CONTAINER="/data/content-repo"
RETENTION_DAYS="${QGP_BACKUP_RETENTION_DAYS:-30}"
REMOTE_URL="${QGP_CONTENT_REMOTE:-}"   # vd: git@gitlab.internal:qgp/content.git (rỗng = bỏ qua push)

mkdir -p "$BACKUP_DIR"
STAMP="$(date +%Y%m%d_%H%M%S)"
TMP="$(mktemp -d)"
trap 'rm -rf "$TMP"' EXIT

echo "[content_backup] copy repo tu container $API_CONTAINER"
docker cp "${API_CONTAINER}:${REPO_IN_CONTAINER}" "$TMP/content-repo"

echo "[content_backup] tao git bundle"
git -C "$TMP/content-repo" bundle create "$BACKUP_DIR/content_${STAMP}.bundle" --all

if [ -n "$REMOTE_URL" ]; then
  echo "[content_backup] push mirror -> $REMOTE_URL"
  git -C "$TMP/content-repo" push --mirror "$REMOTE_URL"
fi

find "$BACKUP_DIR" -name 'content_*.bundle' -type f -mtime +"$RETENTION_DAYS" -delete
echo "[content_backup] done"
# Khôi phục:  git clone content_YYYYmmdd_HHMMSS.bundle content-repo
