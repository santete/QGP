# ADR-0009 — Full-text search: Meilisearch

- **Status:** Accepted
- **Date:** 2026-07-10
- **Deciders:** SA, TL

## Context
KB-F-03 yêu cầu tra cứu < 1s trên corpus vài nghìn tài liệu tiếng Việt (NFR-02).

## Decision
**Meilisearch** (single binary, Rust) cho full-text + filter + typo-tolerance tiếng Việt; **Postgres FTS** làm fallback.

## Consequences
**Tích cực:**
- Nhẹ (1 binary), triển khai Ubuntu dễ
- Typo-tolerant & tiếng Việt tốt out-of-box
- Độ trễ < 100ms

**Tiêu cực / đánh đổi:**
- Thêm một component vận hành (nhỏ)

## Alternatives considered
- **Postgres FTS** — Tokenizer tiếng Việt yếu hơn
- **OpenSearch/Elasticsearch** — Nặng, quá mức cho quy mô này

## Related
SDD §1.5, §8, NFR-02, KB-F-03
