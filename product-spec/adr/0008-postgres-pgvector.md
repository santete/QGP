# ADR-0008 — Datastore: PostgreSQL 16 + pgvector

- **Status:** Accepted
- **Date:** 2026-07-10
- **Deciders:** SA, DBA

## Context
Cần DB quan hệ cho metadata/lifecycle + vector store cho RAG; lean; internal R-DECISION ưu tiên SQL.

## Decision
**PostgreSQL 16** cho toàn bộ dữ liệu quan hệ + **pgvector** cho embeddings (P3). Partial-unique index enforce BR-02 (1 Effective/doc); CHECK effective_date≥issue_date (BR-07); partition `audit_log` theo tháng (NFR-03).

## Consequences
**Tích cực:**
- Một datastore duy nhất (lean)
- Enforce invariant nghiệp vụ ở tầng DB
- Backup/vận hành thống nhất

**Tiêu cực / đánh đổi:**
- pgvector không mạnh bằng vector DB chuyên dụng (đủ cho quy mô vài nghìn tài liệu)

## Alternatives considered
- **MongoDB** — Lệch R-DECISION (ưu tiên SQL); khó enforce invariant quan hệ
- **Vector DB riêng (Qdrant/Milvus)** — Thêm component, không cần ở quy mô này

## Related
SDD §2, BR-01, BR-02, BR-07, ADR-0007, internal_rules/02_Naming_Microservice.md
