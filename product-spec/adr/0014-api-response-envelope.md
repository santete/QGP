# ADR-0014 — API response envelope: theo openapi.yaml (lean), lệch ISC 4-field wrapper

- **Status:** Accepted
- **Date:** 2026-07-11
- **Deciders:** SA, TL, BA (PhucDN7)

## Context
`docs/ai/internal_rules/04_API_Response_and_Error.md / R-RESP-STRUCTURE-001` (severity BLOCKER)
yêu cầu MỌI response bọc wrapper 4 field `{ success, data, error, meta }`.

Nhưng contract `product-spec/api/openapi.yaml` (OpenAPI 3.1, đã validate Redocly 0 lỗi,
là **single source** theo BUILD_PLAN §8) và SDD §3.1 quy định envelope **lean**:
- single resource → trả entity trực tiếp (vd `DocumentDetail`)
- list → `{ data: [...], next_cursor }`
- error → `{ error: { code, message, details? } }`

FE TS client (`apps/web/src/api/schema.d.ts`) đã sinh từ openapi.yaml theo shape lean này.

## Decision
qgp-api **tuân theo `openapi.yaml`** (lean envelope), KHÔNG áp R-RESP-STRUCTURE-001 4-field wrapper.

Giải xung đột theo `00_INDEX.md §⚖️` quy tắc #2 (**rule cụ thể thắng rule chung**): openapi.yaml là
hợp đồng cụ thể đã được duyệt cho API này; R-RESP-STRUCTURE là default chung của ISC. Error code vẫn
theo catalog ISC (UPPER_SNAKE) và mọi lỗi vẫn có `code` + `message` (giữ tinh thần 04).

## Consequences
**Tích cực:** không phải sửa openapi đã validate + regen FE client; nhất quán FE↔BE; nhẹ payload.
**Tiêu cực / đánh đổi:** lệch chuẩn nội bộ chung → cần whitelist R-RESP-STRUCTURE-001 cho service qgp-api
khi review; nếu tích hợp với hệ ISC khác cần adapter.

## Alternatives considered
- **Áp 4-field wrapper**: đúng rule chung nhưng phải sửa openapi + regen client + chỉnh slice S4 đã build; phá "single source".

## Related
openapi.yaml, SDD §3.1, internal_rules/04 (R-RESP-STRUCTURE-001), BUILD_PLAN §8
