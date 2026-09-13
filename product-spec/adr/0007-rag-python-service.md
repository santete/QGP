# ADR-0007 — RAG là service Python riêng (hybrid .NET + Python)

- **Status:** Accepted
- **Date:** 2026-07-10
- **Deciders:** SA, TL

## Context
BOT/RAG (P3) cần ecosystem embeddings/LLM mạnh nhất; core backend là .NET. Cân nhắc Semantic Kernel (.NET đơn stack) vs tách service Python.

## Decision
**Kết hợp**: core backend là .NET (ADR-0006); **`qgp-rag` là service Python riêng** (FastAPI + LlamaIndex/LangChain) đảm nhận ingest/embedding/retrieval/generation, `qgp-api` gọi qua **REST nội bộ** với contract rõ ràng. Vector store dùng chung **pgvector** trong Postgres (ADR-0008).

## Consequences
**Tích cực:**
- Tận dụng ecosystem AI Python mạnh nhất cho RAG
- Tách vòng đời P3 khỏi core → scale/kill switch độc lập
- Không ép RAG vào .NET khi Python phù hợp hơn

**Tiêu cực / đánh đổi:**
- Hai ngôn ngữ trong hệ thống (chấp nhận vì RAG isolated, chỉ P3)
- Cần định nghĩa contract .NET↔Python & versioning

## Alternatives considered
- **Semantic Kernel (.NET-only)** — Đơn stack nhưng ecosystem RAG hẹp hơn Python
- **Gọi LLM trực tiếp trong .NET** — Thiếu orchestration/retrieval tooling trưởng thành

## Related
SDD §1.2, §1.3, §14.4, ADR-0006, ADR-0008, OD-4
