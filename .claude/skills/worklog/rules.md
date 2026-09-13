# Skill: Worklog (Obsidian)

> Condensed rules để sinh/duy trì **worklog dự án** theo chuẩn Obsidian.
> Config: `.claude/config/worklog.yaml` · Template: `templates/worklog.md`.

## Nguyên tắc
1. **1 note / ngày / dự án**: `<vault>/projects/<PROJECT>/Worklog-YYYY-MM-DD.md`.
2. **Frontmatter là hợp đồng** — Dataview lọc bằng nó. Luôn có: `title, type: worklog, project: "[[<hub>]]", date, tags`.
3. **Không trùng lặp**: mở lại worklog trong ngày → **APPEND** vào section đúng, KHÔNG tạo file mới.
4. **Link, đừng copy**: trỏ về hub `[[<PROJECT>]]` thay vì nhồi lại toàn bộ trạng thái dự án.
5. **Trung thực**: test fail thì ghi fail (kèm số); quyết định tự đưa ra phải ghi ở mục Decisions.

## Frontmatter schema (bắt buộc)
```yaml
---
title: "<PROJECT> Worklog — YYYY-MM-DD"
type: worklog
project: "[[<PROJECT>]]"
date: YYYY-MM-DD
tags: [<project-lowercase>, worklog]   # + tag stack tuỳ chọn: dotnet, react...
---
```

## 7 section chuẩn (thứ tự cố định)
| # | Section | Nội dung | Obsidian |
|---|---------|----------|----------|
| — | TL;DR | 1 đoạn: làm gì, kết quả, test | `> [!abstract]` |
| 1 | Đầu việc đã làm | task + files (`path`) | `### ✅ Task N — <tên>` |
| 2 | Tính năng hoàn thành | checklist | `- [x]` / `- [ ]` |
| 3 | Quyết định (Decisions) | mỗi quyết định 1 callout, có lý do | `> [!note] DEC-n —` |
| 4 | Gotchas | triệu chứng → nguyên nhân → fix | `> [!warning] GOT-n —` / `> [!tip]` |
| 5 | Cần cải tiến / nợ kỹ thuật | checklist đầu việc tương lai | `- [ ]` |
| 6 | Trạng thái kiểm thử | bảng hạng mục / kết quả | table |
| 7 | Bước tiếp theo | việc kế / quyết định cần chốt | list, link `[[hub]]` |

## Callout mapping (nếu `use_callouts: true`)
- `[!abstract]` TL;DR · `[!note]` quyết định · `[!warning]` gotcha/bug · `[!tip]` mẹo · `[!question]` cần chốt.

## Dataview (hub auto-list — nếu `use_dataview: true`)
Hub `projects/<PROJECT>/<PROJECT>.md` chứa:
```dataview
TABLE WITHOUT ID file.link AS "Worklog", date AS "Ngày", file.mtime AS "Sửa gần nhất"
FROM #<project-lowercase> AND #worklog
SORT date DESC
```
→ worklog mới **tự hiện** nhờ tag khớp. KHÔNG cần sửa mục lục tay.

## Scaffold lần đầu (nếu chưa có)
- Tạo `projects/<PROJECT>/` + hub MOC (overview + status + Dataview worklog list + roadmap).
- Thêm 1 dòng dự án vào `wiki/Index.md` (mục "🏗️ Dự án") nếu chưa có.

## KHÔNG
- ❌ Tạo worklog thứ 2 trong cùng ngày (append thay vào).
- ❌ Bỏ frontmatter/tag (Dataview sẽ không thấy).
- ❌ Nhồi toàn bộ trạng thái dự án vào mỗi worklog (để ở hub).
- ❌ Ghi vào nhầm vault (verify `vault_path` có tồn tại + đúng vault trước khi ghi).
