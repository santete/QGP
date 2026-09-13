# -*- coding: utf-8 -*-
import re, pathlib
p = pathlib.Path(r"C:\Users\nangh\Documents\workplace\ClaudeCode\qms-governance-platform\product-spec\DESIGN_BRIEF_QGP_v0.1.md")
t = p.read_text(encoding="utf-8")
t = t.replace('version: "0.1"', 'version: "0.2"')
t = t.replace(
  "mỗi component Figma ↔ 1 component React; tên & variant khớp để handoff Dev Mode.",
  "mỗi mockup là component React/HTML tự chứa; tên & variant khớp code để dev tái dùng trực tiếp.")
t = t.replace(
  "> Tài liệu **bàn giao cho đội Design**.",
  "> Tài liệu **bàn giao cho khâu Design** (quy trình dùng **Claude** tạo thiết kế → artifact là **code HTML/React**, KHÔNG dùng Figma).")

new13 = """## 13. ⭐ Artifact tao muốn nhận về (Deliverables)

Quy trình dùng **Claude** để tạo thiết kế → **artifact là CODE tự chứa (HTML/React), KHÔNG dùng Figma**.
Lợi thế: khớp thẳng FE React, preview/chạy được ngay, dev tái dùng trực tiếp; enum/field bám OpenAPI.

| # | Artifact | Định dạng (Claude tạo được) | Definition of Done |
|---|---|---|---|
| D1 | IA + user flows | Markdown + sơ đ\u1 ec13\u1 ed3 **Mermaid** | Sitemap §4 + 7 flow §6 |
| D2 | Mockups các màn | **HTML tự chứa** hoặc **React TSX** (1 file/màn hoặc 1 trang gộp) | Đủ màn P1 §12, đủ 5 state §7 + governance cues |
| D3 | Prototype tương tác | **HTML/React artifact click được** | Chạy 7 flow §6 |
| D4 | Design system / component gallery | **1 trang HTML/React** liệt kê mọi component + variant + state (kiểu Storybook) | Khớp §8, tên PascalCase như React |
| D5 | Design tokens | **CSS variables + Tailwind config** (kèm JSON nếu cần) | 7 màu trạng thái + semantic, typography VN, spacing, breakpoints; đạt contrast |
| D6 | Component code | **React + TS (TSX)** hoặc **HTML+CSS** dùng lại được | Tên khớp §8; props theo enum/field openapi.yaml |
| D7 | Responsive | Media query trong code; demo 360/768/1440 | Màn đọc/tra cứu/feedback ≥ 360 |
| D8 | Accessibility | Semantic HTML + ARIA + focus; kèm checklist tự kiểm | WCAG 2.2 AA; trạng thái không-chỉ-màu |
| D9 | Microcopy VN + strings | Chu\u1 ed7i VN inline + file `strings.ts`/`strings.json` tách i18n | Nhãn trạng thái, cảnh báo, empty, l\u1 ed7i theo code |
| D10 | Icon | **inline SVG** | Có ý nghĩa trạng thái |

**Định dạng bàn giao t\u1 ed5ng:** một thư mục **code (HTML/React + CSS tokens + strings + SVG)** chạy/preview được + 1 trang `index` gộp mockup/gallery. **Không cần Figma/redline — code chính là handoff.**

**Definition of Done (nghiệm thu):**
- [ ] Mọi màn P1 đủ 5 state + governance cues (Approved≠Effective, non-Effective, "Sắp áp dụng").
- [ ] Enum/field khớp openapi.yaml (trạng thái, loại feedback, vai trò, metadata).
- [ ] Đạt WCAG 2.2 AA (kèm checklist tự kiểm).
- [ ] Component gallery + tokens (CSS vars/Tailwind) khớp React.
- [ ] Prototype chạy được 7 flow §6.
- [ ] Microcopy VN đã review; strings tách i18n.
- [ ] Responsive ≥ 360 cho màn đọc/tra cứu/feedback.

---
"""
t = re.sub(r"## 13\. .*?\n---\n", new13, t, flags=re.S)
t = t.replace("DESIGN-QGP-001 v0.1 (Draft)", "DESIGN-QGP-001 v0.2 (Draft)")
p.write_text(t, encoding="utf-8")
print("Figma remaining:", t.count("Figma"), "| bytes:", len(t))
