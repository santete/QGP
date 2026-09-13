# ADR-0003 — Metric truy cập aggregate mặc định

- **Status:** Accepted (từ OD-3)
- **Date:** 2026-07-10
- **Deciders:** PO, QA Governance Lead, Legal

## Context
RPT theo dõi truy cập/tra cứu để đo lường; lo ngại biến thành công cụ giám sát cá nhân (privacy, PDPA/NĐ 13-2023). OD-3.

## Decision
Metric truy cập/tra cứu **mặc định aggregate**; theo dõi **per-user CHỈ cho compliance acknowledgement** (nghĩa vụ đọc), không áp dụng cho hành vi duyệt/tra cứu.

## Consequences
**Tích cực:**
- Bảo vệ quyền riêng tư nhân viên
- Giảm rủi ro pháp lý (PDPA)
- Đúng tinh thần 'đo lường, không giám sát'

**Tiêu cực / đánh đổi:**
- Không truy vết được hành vi cá nhân cho mục đích phân tích sâu (chấp nhận)

## Alternatives considered
- **Per-user analytics đầy đủ** — Rủi ro privacy/pháp lý, đi ngược nguyên tắc

## Related
OD-3, PRD §7, §15, BR-10, SDD §6.5, §9
