# ADR-0004 — BOT chỉ dùng corpus DOC Effective + FAQ

- **Status:** Accepted (từ OD-4)
- **Date:** 2026-07-10
- **Deciders:** PO, QA Governance Lead

## Context
BOT RAG (P3) phải trả lời chuẩn, tránh dùng bản nháp/cũ hoặc nội dung KB không normative. OD-4.

## Decision
BOT **chỉ index tài liệu Effective** (loại Draft/Superseded/Retired) **+ FAQ có cấu trúc**; không index KB tự do.

## Consequences
**Tích cực:**
- Câu trả lời bám bản hiệu lực, có trích nguồn (BR-06)
- Giảm nguy cơ hallucination từ nội dung không chuẩn

**Tiêu cực / đánh đổi:**
- Phạm vi trả lời hẹp hơn (chấp nhận — đúng governance)

## Alternatives considered
- **Index cả KB tự do** — Rủi ro nội dung không chuẩn
- **Index mọi trạng thái** — Trả lời theo bản sai

## Related
OD-4, BOT-F-02, BR-06, SDD §1.4, §4.4, ADR-0007
