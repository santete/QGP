# @qgp/web — Quality Governance Portal (Frontend)

React + Vite + TypeScript, hệ thiết kế **Minimal UI** (green `#00A76F`, Inter, radius card 16 / nút 8, shadow-not-border, dark mode). Nối `qgp-api` (.NET 8) qua REST `/v1`.

## Sprint 1 — vertical slice
Màn **Chi tiết tài liệu (S4)**: `GET /documents/{doc_id}` → render Markdown (sanitize) + `StatusBadge` (7 trạng thái) + `EffectiveDateBadge` + `NonEffectiveBanner` + acknowledge (DOC-F-09).

Dev chạy với **mock API** (`VITE_USE_MOCK=1`) — không cần BE thật. Tài liệu mẫu: `QA-PROC-005`.

## Lệnh
```bash
npm install
npm run gen:api    # sinh src/api/schema.d.ts từ product-spec/api/openapi.yaml (single source)
npm run dev        # http://localhost:5173 → /documents/QA-PROC-005
npm run typecheck
npm test
npm run build
```

## Cấu trúc
- `src/theme/tokens.css` — design tokens đồng bộ từ Claude Design (Minimal UI). **Không sửa tay giá trị.**
- `src/api/` — `schema.d.ts` (generated), `client.ts` (fetch wrapper + ApiError), `mock.ts` (fixture dev), `types.ts` (alias).
- `src/components/` — DS core Minimal.
- `src/features/documents/` — slice S4.
- `src/i18n/strings.ts` — chuỗi tiếng Việt.

## Governance
- Enum/field FE lấy **duy nhất** từ `openapi.yaml` (chạy `gen:api`, không hardcode).
- `content_html` luôn sanitize (DOMPurify) trước khi nhúng — chống XSS.
- Token = single source từ design; Tailwind chỉ map CSS var.
