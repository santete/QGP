# ADR-0010 — Frontend: React 18 + Vite + TypeScript

- **Status:** Accepted
- **Date:** 2026-07-10
- **Deciders:** SA, TL

## Context
Portal cần UX động (search, dashboard, panel 'Tài liệu dành cho bạn'), responsive, tiếng Việt.

## Decision
**React 18 + Vite + TypeScript** (SPA) gọi REST `/v1`.

## Consequences
**Tích cực:**
- Ecosystem lớn, UX richness
- Kỹ năng phổ biến, dễ tuyển/bàn giao
- Responsive/mobile tốt

**Tiêu cực / đánh đổi:**
- Build tooling riêng; hai codebase (web/api)

## Alternatives considered
- **Blazor** — Đơn stack .NET nhưng ecosystem UI hẹp hơn
- **Server-render + htmx** — Đơn giản nhưng UX hạn chế cho dashboard

## Related
SDD §1.5, NFR-08
