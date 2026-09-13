---
title: "UI/UX Design Requirements & Handoff Brief — Quality Governance Portal (QGP)"
doc_id: DESIGN-QGP-001
version: "0.2"
status: DRAFT
owner: "BA/PM (PhucDN7) — bàn giao đội Design"
audience: "Product Designer, UX/UI, Design System, (Dev FE để đối chiếu)"
sources:
  - "PRD-QGP-001 v1.0 (Approved)"
  - "SDD-QGP-001 v1.0"
  - "API: product-spec/api/openapi.yaml (operationId là hợp đồng dữ liệu)"
date_created: 2026-07-10
---

# UI/UX Design Requirements & Handoff Brief — QGP

> Tài liệu **bàn giao cho khâu Design** (quy trình dùng **Claude** tạo thiết kế → artifact là **code HTML/React**, KHÔNG dùng Figma). Mô tả *cần thiết kế cái gì, ràng buộc gì, để khớp kiến trúc đã chốt*
> (React + REST /v1, docs-as-code, RBAC, state machine tài liệu). Phần **§13 nêu rõ artifact cần nhận về**.
> Đây KHÔNG phải bản thiết kế — là yêu cầu đầu vào để Design tạo wireframe → hi-fi → prototype → handoff.

---

## 1. Bối cảnh & Nguyên tắc thiết kế

**Sản phẩm:** Portal nội bộ FPT ISC quản trị tài liệu quy trình QA — một nguồn sự thật, kiểm soát version
và ngày hiệu lực, tra cứu nhanh, feedback, onboarding, báo cáo, trợ lý AI (P3).

**Design principles (bắt buộc tuân thủ — trích PRD):**
1. **Low-friction** — đóng góp/feedback/tra cứu phải dễ hơn im lặng. Form tối thiểu, mặc định thông minh.
2. **Approved ≠ Effective phải nhìn thấy được** — luôn hiển thị trạng thái + ngày áp dụng; cảnh báo rõ khi
   xem bản không phải Effective. Đây là *yêu cầu governance sống còn*, không phải trang trí.
3. **Explainable** — gợi ý (REC) và câu trả lời BOT phải kèm **lý do/nguồn**; không hộp đen.
4. **Traceability & trust** — mọi tài liệu thể hiện version, ngày, nguồn; BOT luôn trích dẫn.
5. **AI-first readability** — nội dung là Markdown; giao diện đọc phải tối ưu cho văn bản dài, mục lục, anchor.
6. **Role-aware** — mỗi vai trò chỉ thấy hành động trong quyền (RBAC), UI không phô nút không dùng được.

---

## 2. Người dùng & UI theo vai trò (RBAC)

Persona (PRD §6): **Reader/New joiner** (tra cứu, onboard) · **Author/Approver** (soạn, duyệt) ·
**QA Governance Lead/Admin** (quản trị, báo cáo).

| Vai trò | Thấy gì / làm gì trên UI | Ảnh hưởng thiết kế |
|---|---|---|
| Reader (mọi NV) | Tra cứu, đọc Effective, feedback, xác nhận đã đọc, gợi ý theo role, hỏi BOT | UI đọc-là-chính; ẩn nút soạn/duyệt |
| Contributor | + đóng góp KB | Hiện editor KB, không hiện DOC lifecycle |
| Author | + soạn/sửa DOC, submit, publish | Hiện editor metadata + Markdown, publish dialog |
| Approver | + duyệt/từ chối | Hiện review queue + action bar approve/reject |
| QA Lead | + cấu hình, báo cáo, taxonomy, RACI | Hiện admin + dashboards |
| System Admin | + RBAC, kỹ thuật | Hiện quản trị hệ thống |

> Thiết kế **1 hệ thống, hiển thị theo quyền** (progressive disclosure) — không làm nhiều app rời.

---

## 3. Ràng buộc thiết kế (bắt buộc)

| # | Ràng buộc | Chi tiết |
|---|---|---|
| C1 | Ngôn ngữ | Tiếng Việt là chính (UI copy). Chuẩn bị i18n (tách chuỗi) để thêm EN sau |
| C2 | Responsive | Desktop-first nhưng **dùng được trên mobile ≥ 360px** (NFR-08); màn đọc & tra cứu ưu tiên mobile |
| C3 | Accessibility | **WCAG 2.2 AA**: contrast ≥ 4.5:1, keyboard đầy đủ, focus rõ, ARIA, không dùng-chỉ-màu, reduced motion |
| C4 | Công nghệ | FE = **React 18 + Vite + TS**. Thiết kế theo **component + design tokens** để map thẳng sang code |
| C5 | Nội dung | Render **Markdown** (tài liệu dài): mục lục, heading anchor, bảng, code block, hình; typography đọc lâu |
| C6 | Governance visual | Hệ **badge trạng thái** nhất quán (7 trạng thái) + badge "Sắp áp dụng" + banner cảnh báo non-Effective/Superseded |
| C7 | Hiệu năng cảm nhận | Search mục tiêu < 1s: cần **skeleton/loading**; thao tác ack/feedback dùng **optimistic UI** |
| C8 | Phân tách dữ liệu | Dashboard vs dữ liệu thô ở màn/tab khác nhau khi dashboard > ~10 dòng (PRD §7) |
| C9 | Quyền riêng tư | Metric hiển thị **aggregate**; KHÔNG show hành vi cá nhân (OD-3) trừ phần compliance acknowledgement |
| C10 | Degrade theo phase | REC/BOT tắt ở P1 (feature flag) → UI phải ẩn/khoá mượt, không vỡ layout |

---

## 4. Kiến trúc thông tin (IA) & Điều hướng

Top-nav (role-aware): **Trang chủ · Tài liệu (DOC) · Wiki (KB) · Onboarding · Báo cáo · Quản trị · Trợ lý (BOT)**.
Global: **thanh tìm kiếm** (mọi trang), avatar/role, thông báo (subscription), nút feedback in-context.

Sitemap tối thiểu:
- Trang chủ → "Tài liệu dành cho bạn" (REC) + việc cần đọc + tài liệu mới/đổi
- Tài liệu → kết quả tìm/duyệt → chi tiết tài liệu → lịch sử/diff; (Author) soạn/sửa; (Approver) hàng đợi duyệt
- Wiki → cây chủ đề → trang KB (nhúng DOC)
- Onboarding → learning path theo role → checklist
- Báo cáo → dashboards (ban hành/sức khoẻ/truy cập/compliance/feedback)
- Quản trị → RBAC, taxonomy/tag, audit log, RACI
- Trợ lý → chat BOT (P3)

---

## 5. Danh sách màn hình cần thiết kế (screen inventory)

Mỗi màn map tới US/UC (PRD) và endpoint (openapi.yaml operationId) để khớp dữ liệu.

| # | Màn hình | Vai trò | US/UC | API (operationId) | Phase |
|---|---|---|---|---|---|
| S1 | Đăng nhập SSO / chuyển hướng / không có quyền | all | UC-20 | (OIDC) | P1 |
| S2 | Trang chủ + panel **"Tài liệu dành cho bạn"** (must_read vs suggested + chip lý do) | Reader | UC-23 | getRecommendations | P2 |
| S3 | Tìm kiếm + filter (tag/loại/phân hệ) + kết quả (badge trạng thái) | Reader | UC-07 | search, listDocuments | P1 |
| S4 | **Chi tiết tài liệu** (Markdown, badge trạng thái + ngày áp dụng, mục lục, tài liệu liên quan, nút feedback, nút "Tôi đã đọc") | Reader | UC-07/10 | getDocument, acknowledgeVersion | P1 |
| S5 | Cảnh báo **non-Effective / Superseded** + link tới bản Effective | Reader | UC-08 | getDocument | P1 |
| S6 | **Lịch sử phiên bản + Diff** 2 version | Reader/Author | UC-09 | listVersions, getDiff | P1 |
| S7 | **Soạn/sửa tài liệu**: form metadata (doc_id khoá, type, classification, audience_roles, mandatory_ack, next_review_date) + editor Markdown (preview) | Author | UC-01/05/06 | createDocument, createVersion | P1 |
| S8 | **Publish dialog**: issue_date, effective_date, change_summary (validate BR-04/07, badge "Sắp áp dụng") | Author | UC-03 | publishVersion | P1 |
| S9 | **Hàng đợi duyệt** + màn review (xem + diff) + action bar Approve/Reject (bắt buộc comment khi reject) | Approver | UC-02 | submitVersion, approveVersion | P1 |
| S10 | Trang **Wiki (KB)** + nhúng DOC (hiện version/trạng thái) + cảnh báo superseded | Reader/Contributor | UC-13 | (KB endpoints) | P1 |
| S11 | Trang "**Bắt đầu từ đâu**" theo role | New joiner | KB-F-04 | getRecommendations | P1/P2 |
| S12 | **Onboarding**: learning path + checklist % + dashboard cá nhân "cần đọc/đã đọc" | New joiner | UC-14 | (ONB endpoints) | P2 |
| S13 | **Feedback in-context** (popover: dropdown loại + ô mô tả tùy chọn, tự đính kèm ngữ cảnh) | Reader | UC-15 | createFeedback | P1 |
| S14 | **Feedback triage board** (New→Triaged→InProgress→Resolved/Rejected) | QA | UC-15/16 | triageFeedback | P1/P2 |
| S15 | **Dashboards báo cáo**: ban hành, sức khoẻ (quá hạn review), truy cập (aggregate), compliance (ack), feedback | QA Lead | UC-22 | getIssuanceReport, getComplianceReport | P2 |
| S16 | **Audit log viewer** (lọc theo tài liệu/người/thời gian) | QA Lead/Auditor | UC-21 | listAudit | P1 |
| S17 | **Quản trị**: RBAC/vai trò, taxonomy/tag, cấu hình RACI phê duyệt | Admin/QA Lead | UC-19 | (ADM endpoints) | P1/P2 |
| S18 | **Chat BOT** (câu hỏi, trả lời + **citation card**, trạng thái "không chắc", nút chuyển thành feedback) | Reader | UC-17/18 | askAssistant | P3 |
| S19 | Thông báo & subscription (theo dõi tài liệu/chủ đề) | Reader | ADM-F-04 | (notif) | P2 |

---

## 6. Luồng cần dựng prototype (clickable)

1. **Tra cứu → mở Effective** (S3→S4) và **mở nhầm bản cũ → cảnh báo → về Effective** (S5) — UC-07/08
2. **Soạn → submit → duyệt → publish → (tự) Effective** (S7→S9→S8) — UC-01/02/03/04
3. **Major revision + reset acknowledgement** (S7→S9→S8, có cảnh báo impacts) — UC-06
4. **Onboarding learning path + xác nhận đã đọc** (S12→S4) — UC-14
5. **Feedback in-context → QA triage** (S13→S14) — UC-15
6. **Panel gợi ý theo role** (S2) — UC-23
7. **BOT hỏi–đáp có trích nguồn / "không chắc"** (S18) — UC-17/18

---

## 7. Trạng thái & edge case phải thiết kế cho MỌI màn

| State | Yêu cầu |
|---|---|
| Loading | Skeleton (đặc biệt search, dashboard, chi tiết tài liệu) |
| Empty | Không kết quả tìm / chưa có gợi ý / chưa có feedback — kèm CTA hợp lý |
| Error | Theo **error catalog** (SDD §3.3): 403 ngoài quyền, 409 xung đột trạng thái/immutable, 422 validate (BR-04/07), 503 BOT tắt |
| Success | Toast + cập nhật optimistic (ack, feedback, publish) |
| Governance cues | Badge 7 trạng thái; badge "Sắp áp dụng dd/mm/yyyy"; banner non-Effective; banner Superseded; nhãn "Bắt buộc đọc" |
| Permission-limited | Ẩn/disable hành động ngoài quyền + tooltip lý do |
| Mobile | Bố cục ≥ 360px cho S3/S4/S13 (tra cứu, đọc, feedback) tối thiểu |

---

## 8. Design system & Component inventory (map sang React)

Yêu cầu một **design system** (không thiết kế rời từng màn). Component tối thiểu — đặt tên **PascalCase khớp React**:

| Component | Vai trò | Ghi chú state/variants |
|---|---|---|
| StatusBadge | 7 trạng thái tài liệu | Màu semantic; kèm icon + text (không chỉ màu — C3) |
| EffectiveDateBadge | "Đang hiệu lực" / "Sắp áp dụng dd/mm" | 2 variant |
| NonEffectiveBanner | Cảnh báo bản cũ/nháp + link Effective | error/warning tone |
| DocumentCard | Kết quả tìm / item gợi ý | có reasonChip cho REC (bắt buộc/đọc lại/khớp role/mới) |
| VersionTimeline + DiffViewer | Lịch sử + so sánh | diff thêm/bớt, accessible |
| MetadataForm | Form metadata tài liệu | doc_id disabled (immutable), date pickers, enum select, audience_roles multiselect, tags |
| MarkdownEditor + MarkdownView | Soạn + đọc | preview song song; TOC/anchor; sanitize |
| PublishDialog | issue/effective_date + change_summary | inline validation BR-04/07 |
| ApprovalActionBar | Approve/Reject + comment | reject bắt buộc comment |
| FeedbackPopover | dropdown loại + textarea tùy chọn | tối thiểu 1 field (low-friction) |
| RecommendationPanel | must_read vs suggested | nhóm rõ ràng + reason |
| SearchBar + FilterChips | tìm + lọc | debounce, loading |
| LearningPathChecklist | tiến độ % | tick, trạng thái đã đọc |
| DashboardTile / Chart | KPI + biểu đồ aggregate | tách dữ liệu thô (C8); theo chuẩn dataviz nội bộ |
| AuditTable | bảng audit | filter, phân trang cursor |
| ChatMessage + CitationCard | BOT | citation: doc_id + version + ngày + link |
| Toast, Dialog, EmptyState, ErrorState, Skeleton | dùng chung | — |
| AppNav (role-aware) | điều hướng | ẩn mục theo quyền |

**Design tokens (bắt buộc, export được):**
- **Color**: bảng semantic cho 7 trạng thái + success/warning/error/info + neutral; đạt contrast AA; nêu rõ có dark mode hay không.
- **Typography**: font hỗ trợ tiếng Việt đầy đủ dấu; scale cho đọc văn bản dài (body 16px+), heading, mono cho code.
- **Spacing/radius/elevation/breakpoints** (360/768/1024/1440).
- Token đặt tên theo **W3C Design Tokens** hoặc Style Dictionary để dev import (§13).

---

## 9. Content & Microcopy (tiếng Việt)

- Tone: rõ ràng, ngắn, nghiệp vụ QA; tránh thuật ngữ kỹ thuật với Reader.
- Cần **content sheet (VN)** cho: nhãn 7 trạng thái, badge "Sắp áp dụng", cảnh báo non-Effective/Superseded,
  nhãn "Bắt buộc đọc", empty states, và **thông điệp lỗi theo từng code** (403/409/422/503) — thân thiện, gợi hành động.
- Tách chuỗi để i18n (C1).

---

## 10. Accessibility (WCAG 2.2 AA) — nghiệm thu bắt buộc

- Contrast ≥ 4.5:1 (text), ≥ 3:1 (UI lớn/icon trạng thái).
- **Keyboard đầy đủ** cho mọi flow §6; focus order hợp lý; focus ring rõ.
- Trạng thái **không chỉ bằng màu** (kèm icon/label). ARIA cho badge, toast, dialog, bảng diff/audit.
- Screen reader đọc được diff & bảng; tôn trọng reduced-motion. Kèm **báo cáo kiểm tra a11y** khi bàn giao.

---

## 11. Cách khớp kiến trúc (để "adapt")

- **Component-driven**: mỗi mockup là component React/HTML tự chứa; tên & variant khớp code để dev tái dùng trực tiếp.
- **Dữ liệu là hợp đồng**: enum trạng thái, loại feedback, vai trò, field metadata **phải khớp** openapi.yaml
  + DDL (SDD §2). Không tự đặt trạng thái/loại mới ngoài spec — nếu cần, raise để cập nhật PRD/spec trước.
- **Tokens → code**: xuất tokens (JSON) để dev nạp theme (CSS vars/Tailwind).
- **Feature flags theo phase**: P1 không có REC/BOT → thiết kế trạng thái ẩn/khoá; đánh dấu rõ màn P1/P2/P3.
- **Hiệu năng cảm nhận**: skeleton cho search/dashboard; optimistic cho ack/feedback; tránh layout shift.
- **Markdown**: thống nhất style đọc (heading, bảng, callout, code) vì nội dung do docs-as-code sinh.

---

## 12. Phạm vi theo phase (ưu tiên thiết kế)

- **P1 (MVP, ưu tiên cao nhất)**: S1, S3, S4, S5, S6, S7, S8, S9, S10, S13, S16, S17 + design system core.
- **P2**: S2 (REC), S11, S12 (ONB), S14, S15 (RPT dashboards), S19 (notif).
- **P3**: S18 (BOT chat + citation).

---

## 13. ⭐ Artifact tao muốn nhận về (Deliverables)

Quy trình dùng **Claude** để tạo thiết kế → **artifact là CODE tự chứa (HTML/React), KHÔNG dùng Figma**.
Lợi thế: khớp thẳng FE React, preview/chạy được ngay, dev tái dùng trực tiếp; enum/field bám OpenAPI.

| # | Artifact | Định dạng (Claude tạo được) | Definition of Done |
|---|---|---|---|
| D1 | IA + user flows | Markdown + sơ đồ **Mermaid** | Sitemap §4 + 7 flow §6 |
| D2 | Mockups các màn | **HTML tự chứa** hoặc **React TSX** (1 file/màn hoặc 1 trang gộp) | Đủ màn P1 §12, đủ 5 state §7 + governance cues |
| D3 | Prototype tương tác | **HTML/React artifact click được** | Chạy 7 flow §6 |
| D4 | Design system / component gallery | **1 trang HTML/React** liệt kê component + variant + state (kiểu Storybook) | Khớp §8, tên PascalCase như React |
| D5 | Design tokens | **CSS variables + Tailwind config** (JSON nếu cần) | 7 màu trạng thái + semantic, typography VN, spacing, breakpoints; đạt contrast |
| D6 | Component code | **React TSX** hoặc **HTML+CSS** dùng lại được | Tên khớp §8; props theo enum/field openapi.yaml |
| D7 | Responsive | Media query trong code; demo 360/768/1440 | Màn đọc/tra cứu/feedback ≥ 360 |
| D8 | Accessibility | Semantic HTML + ARIA + focus; kèm checklist tự kiểm | WCAG 2.2 AA; trạng thái không-chỉ-màu |
| D9 | Microcopy VN + strings | Chuỗi VN inline + strings.ts/strings.json tách i18n | Nhãn trạng thái, cảnh báo, empty, lỗi theo code |
| D10 | Icon | **inline SVG** | Có ý nghĩa trạng thái |

**Định dạng bàn giao tổng:** một thư mục **code (HTML/React + CSS tokens + strings + SVG)** chạy/preview được + 1 trang index gộp mockup/gallery. **Không cần Figma/redline — code chính là handoff.**

**Definition of Done (nghiệm thu):**
- [ ] Mọi màn P1 đủ 5 state + governance cues (Approved != Effective, non-Effective, Sắp áp dụng).
- [ ] Enum/field khớp openapi.yaml (trạng thái, loại feedback, vai trò, metadata).
- [ ] Đạt WCAG 2.2 AA (kèm checklist tự kiểm).
- [ ] Component gallery + tokens (CSS vars/Tailwind) khớp React.
- [ ] Prototype chạy được 7 flow §6.
- [ ] Microcopy VN đã review; strings tách i18n.
- [ ] Responsive ≥ 360 cho màn đọc/tra cứu/feedback.

---

## 14. Tao sẽ cung cấp cho Design (đầu vào)

- Bản brief này + **PRD** (persona, US/UC, MoSCoW) + **SDD** (flow, state machine, RBAC, deployment) +
  **OpenAPI** (openapi.yaml — nguồn enum/field).
- Danh sách trạng thái/loại/vai trò chuẩn (từ spec) để không lệch.
- Point of contact: BA/PM (PhucDN7) chốt nghiệp vụ; SA/TL chốt ràng buộc kỹ thuật.

---

## 15. Câu hỏi mở cho Design (cần chốt sớm)

| # | Câu hỏi | Ảnh hưởng |
|---|---|---|
| Q1 | Có **brand guideline / logo / palette** nội bộ FPT ISC để bám? | Nền tảng tokens màu/typography |
| Q2 | **Dark mode** có làm ở P1 không? | Gấp đôi khối lượng token/mockup |
| Q3 | Thư viện component nền (tự build vs Radix/shadcn/MUI)? | Tốc độ & nhất quán |
| Q4 | Chuẩn **dataviz** cho dashboard (màu/loại biểu đồ)? | S15 |
| Q5 | Mức ưu tiên mobile (chỉ đọc/tra cứu hay toàn bộ)? | Phạm vi responsive |
| Q6 | Font tiếng Việt ưu tiên (vd Inter/Be Vietnam Pro)? | Typography tokens |

---

## 16. References
- PRD: product-spec/PRD_QGP_v1.0.docx
- SDD: product-spec/SDD_QGP_v1.0.md (flow/state/RBAC/deployment)
- API (nguồn enum/field): product-spec/api/openapi.yaml
- ADR: product-spec/adr/

---

*DESIGN-QGP-001 v0.2 (Draft) · bàn giao đội Design · map từ PRD v1.0 + SDD v1.0 · Nội bộ FPT ISC*
