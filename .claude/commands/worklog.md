---
description: Ghi worklog dự án ra Obsidian vault (chuẩn frontmatter + callouts + Dataview). Tạo mới hoặc append entry trong ngày.
---

Khi user gõ `/worklog`, sinh/cập nhật **worklog dự án** trong Obsidian vault. Thực hiện tuần tự, KHÔNG skip.

> Load rules format TRƯỚC: đọc `.claude/skills/worklog/rules.md` (7 section chuẩn + callout + Dataview).

## Bước 0 — Load config

Đọc `.claude/config/worklog.yaml`. Lấy: `vault_path`, `project`, `author`, `project_folder`, `hub_note`, `worklog_name`, `use_callouts`, `use_dataview`, `sources`.

- Nếu file config KHÔNG tồn tại → hỏi user 2 câu: **vault path** + **tên project**, rồi tạo config từ mẫu, mới đi tiếp.
- Resolve placeholder: `{project}` → giá trị `project`; `{date}` → hôm nay (`date +%Y-%m-%d`).

## Bước 1 — Verify vault (BẮT BUỘC — tránh ghi nhầm)

```bash
ls -d "<vault_path>/.obsidian" 2>/dev/null && echo "OK vault" || echo "SAI: không phải vault"
```

Nếu SAI → STOP, báo user (thường do nhầm 2 folder gần tên nhau, vd `ObsidianVault` vs `Obsidian Vault`). KHÔNG đoán vault khác.

## Bước 2 — Gom việc trong session

Theo `sources` trong config:
1. `git diff --name-only HEAD 2>/dev/null` → danh sách files đã đổi.
2. Đọc `.claude/memory/project_state.yaml` → `last_successful_task`, `completed_tasks` (mới nhất), `decisions`, `known_gotchas`, `pending_tasks`.
3. Nếu context session đủ rõ → tự soạn 7 section. Nếu thiếu → hỏi user **1 câu**: "Tóm tắt việc hôm nay + kết quả test?"

Map dữ liệu vào 7 section (theo `rules.md`): TL;DR · Đầu việc · Tính năng · Decisions · Gotchas · Tech-debt · Test · Next.

## Bước 3 — Scaffold nếu chưa có (lần đầu / project mới)

Tính đường dẫn: `HUB = <vault_path>/<project_folder>/<hub_note>.md`, `WL = <vault_path>/<project_folder>/<worklog_name>.md`.

- Nếu `<project_folder>` chưa có → tạo.
- Nếu HUB chưa có → tạo hub MOC: frontmatter (`type: MOC`), overview 1 dòng, section Trạng thái, **Dataview worklog list** (nếu `use_dataview`), roadmap (từ `pending_tasks`).
- Nếu `wiki/Index.md` chưa có dòng dự án này → thêm 1 dòng vào mục "🏗️ Dự án" (tạo mục nếu chưa có).

## Bước 4 — Ghi worklog (create HOẶC append)

- **Nếu `WL` CHƯA tồn tại** → tạo từ `.claude/skills/worklog/templates/worklog.md`, thay placeholder (`{{PROJECT}}`, `{{DATE}}`, `{{project_tag}}` = project lowercase, và 7 khối nội dung).
- **Nếu `WL` ĐÃ tồn tại (đã ghi trong ngày)** → **APPEND**, KHÔNG tạo file mới:
  - Đọc file, thêm entry mới vào đúng section (vd Task mới vào mục 1, gotcha mới vào mục 4).
  - Cập nhật TL;DR + mục 6 (test) cho khớp trạng thái mới nhất.
- Tuân thủ format ở `rules.md`: callout (`use_callouts`), tag khớp Dataview, link `[[<hub>]]`.

## Bước 5 — Report (format BẮT BUỘC)

```
📓 WORKLOG SAVED

📁 File:     <project_folder>/<worklog_name>.md  (<created|appended>)
🗂️  Hub:      [[<hub_note>]]  (<created|updated|unchanged>)
✍️  Sections: <các mục đã ghi/cập nhật>
🏷️  Tags:     <project-lowercase>, worklog
🔗 Dataview: worklog sẽ tự hiện ở hub (nếu đã cài Dataview)

💡 Mở [[<hub_note>]] trong Obsidian để xem danh sách tự cập nhật.
```

## KHÔNG làm

- ❌ Tạo file worklog thứ 2 trong cùng ngày → phải APPEND (Bước 4).
- ❌ Ghi khi Bước 1 báo SAI vault → dừng, hỏi user.
- ❌ Bỏ frontmatter/tag → Dataview mất tác dụng.
- ❌ Copy toàn bộ trạng thái dự án vào mỗi worklog → để ở hub, worklog chỉ ghi việc trong ngày.
- ❌ Tự bịa test pass — lấy số thật từ session (git/test output).

## Khi nào dùng

| Trường hợp | Dùng |
|---|---|
| Ghi lại việc đã làm hôm nay ra Obsidian | `/worklog` |
| Cập nhật lần 2 trong ngày (thêm task/gotcha) | `/worklog` (auto append) |
| Đồng bộ trạng thái/roadmap dự án lên hub | `/worklog-sync` |
| Đóng session + ghi memory framework (KHÔNG phải Obsidian) | `/done` hoặc `/rotate` |

> `/worklog` xuất ra **Obsidian** (cho người đọc). `/done` `/rotate` ghi **`.claude/memory`** (cho AI session sau). Hai thứ bổ trợ, không thay nhau.
