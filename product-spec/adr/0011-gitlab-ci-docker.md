# ADR-0011 — CI/CD: GitLab CI + Docker trên Ubuntu

- **Status:** Accepted
- **Date:** 2026-07-10
- **Deciders:** SA, DevOps

## Context
Internal MR convention dùng GitLab; host Ubuntu nội bộ; team lean.

## Decision
**GitLab CI** (validate → build → test → package → deploy), **Docker + docker-compose**, **Nginx + TLS** trên Ubuntu. MR theo `01_MR_Compliance` (Conventional Commits, tag [AI]).

## Consequences
**Tích cực:**
- Hợp convention MR nội bộ
- Đơn giản, phù hợp quy mô
- Validate frontmatter/BR ngay ở pipeline

**Tiêu cực / đánh đổi:**
- Chưa dùng orchestrator (k8s) — chấp nhận ở quy mô nội bộ

## Alternatives considered
- **GitHub Actions** — Lệch hạ tầng GitLab nội bộ
- **Kubernetes** — Quá mức cho quy mô hiện tại

## Related
SDD §11, internal_rules/01_MR_Compliance.md
