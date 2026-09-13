# ADR-0005 — Reset acknowledgement khi major version

- **Status:** Accepted (từ OD-5)
- **Date:** 2026-07-10
- **Deciders:** PO, QA Governance Lead

## Context
Tài liệu bắt buộc đọc; khi đổi normative (major) người dùng cần đọc & xác nhận lại. OD-5.

## Decision
**Major (x.0)** = thay đổi normative → **reset acknowledgement + phê duyệt lại**. **Minor (x.y)** = editorial → owner tự phát hành, **không** reset.

## Consequences
**Tích cực:**
- Đảm bảo người dùng nắm quy tắc mới nhất
- Bằng chứng compliance chính xác theo version

**Tiêu cực / đánh đổi:**
- Người dùng phải đọc lại khi có major (đúng mục tiêu)

## Alternatives considered
- **Không reset khi major** — Rủi ro tiếp tục áp dụng quy tắc cũ

## Related
OD-5, BR-05, BR-08, DOC-F-09, SDD §5.3
