# ADR-0012 — Secrets: HashiCorp Vault

- **Status:** Accepted
- **Date:** 2026-07-10
- **Deciders:** SA, DevOps, CSOC

## Context
Cần quản lý secret (OIDC client secret, LLM API key, DB creds, Meilisearch/Redis) an toàn; open-source; hook chặn hardcoded secret.

## Decision
**HashiCorp Vault** (prod) + **GitLab masked/protected variables** (CI) + **.NET Secret Manager** (dev); ưu tiên dynamic DB credentials; không commit secret vào Git/log.

## Consequences
**Tích cực:**
- Tập trung, hỗ trợ rotation
- Open-source, chạy Ubuntu
- Khớp hook post-write-check (chặn hardcoded secret)

**Tiêu cực / đánh đổi:**
- Vận hành thêm Vault

## Alternatives considered
- **SOPS/age (file-based)** — Khó rotation tập trung
- **Env var thuần** — Kém an toàn, khó audit

## Related
SDD §6.4
