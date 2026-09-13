# ADR-0006 — Backend: .NET 8 ASP.NET Core

- **Status:** Accepted
- **Date:** 2026-07-10
- **Deciders:** SA, TL

## Context
Cần backend cho lifecycle/RBAC/API. Internal Coding Convention ưu tiên .NET; host Ubuntu; team lean.

## Decision
`qgp-api` = **.NET 8 ASP.NET Core (Minimal API)**, EF Core + Npgsql, FluentValidation, Serilog, Polly, Quartz.NET.

## Consequences
**Tích cực:**
- Hợp internal Coding Convention (§06)
- Cross-platform, chạy tốt trên Ubuntu
- Hiệu năng & ecosystem tốt

**Tiêu cực / đánh đổi:**
- Team cần kỹ năng .NET

## Alternatives considered
- **Python FastAPI** — Lệch Coding Convention nội bộ cho core backend
- **Node NestJS** — Không có lợi thế rõ so với .NET ở đây

## Related
SDD §1.5, internal_rules/06_Coding_Convention.md, ADR-0007
