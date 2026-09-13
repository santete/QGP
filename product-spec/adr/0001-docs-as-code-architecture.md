# ADR-0001 — Docs-as-code architecture

- **Status:** Accepted (từ OD-1)
- **Date:** 2026-07-10
- **Deciders:** PO, SA, BA

## Context
Tài liệu quy trình QA rải rác (Excel/HTML/Markdown/ổ chia sẻ), không rõ bản nào Effective, khó version, cần AI-readable cho BOT. PRD OD-1 so sánh 3 hướng: docs-as-code / Confluence-SharePoint / custom app.

## Decision
Dùng **docs-as-code**: nội dung normative là Markdown + frontmatter lưu trong **Git** (bất biến theo commit); QGP là **web app động** render Markdown server-side và quản lý metadata/lifecycle/RBAC/feedback/recommendation trong DB. KHÔNG dùng static site generator thuần (MkDocs/Docusaurus) vì không đáp ứng RBAC/workflow/badge per-user.

## Consequences
**Tích cực:**
- Version tự nhiên bằng Git — bất biến (BR-03, NFR-05)
- Export Markdown gốc, tránh vendor lock-in (NFR-06)
- Corpus structured → BOT index chính xác
- Lean, open-source, hợp triết lý AI-first

**Tiêu cực / đánh đổi:**
- Cần một lớp UI cho non-dev thao tác thay vì Git trực tiếp (R5)
- Cần CI validate frontmatter/metadata

## Alternatives considered
- **Confluence/SharePoint + plugin** — Yếu ở Approved≠Effective, kiểm soát ngày hiệu lực, AI-readable; vendor lock-in
- **Custom app từ đầu** — Tốn nhất, over-engineering, time-to-market chậm
- **Static site generator thuần** — Không làm được RBAC/workflow/feedback/recommendation per-user

## Related
PRD §3, OD-1, SDD §1, ADR-0006, ADR-0008
