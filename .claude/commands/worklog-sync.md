---
description: Đồng bộ trạng thái + roadmap dự án từ .claude/memory lên hub MOC trong Obsidian vault.
---

Khi user gõ `/worklog-sync`, cập nhật **hub MOC dự án** (không tạo worklog entry). Dùng khi trạng thái/roadmap đổi mà không cần ghi nhật ký ngày.

> Load `.claude/skills/worklog/rules.md` + `.claude/config/worklog.yaml` trước.

## Bước 1 — Load & verify
- Đọc config → `vault_path`, `project`, `project_folder`, `hub_note`. Verify vault (`ls -d "<vault_path>/.obsidian"`). Sai → STOP.
- `HUB = <vault_path>/<project_folder>/<hub_note>.md`. Nếu HUB chưa có → tạo mới (như Bước 3 của `/worklog`).

## Bước 2 — Gom trạng thái từ memory
Đọc `.claude/memory/project_state.yaml`:
- `current_sprint`, `last_successful_task`, `completed_tasks` → phần "Đã xong".
- `pending_tasks` → phần "Còn lại / roadmap" (checklist `- [ ]`, giữ priority).
- (nếu có) test/metrics gần nhất → dòng trạng thái.

## Bước 3 — Cập nhật hub (in-place, KHÔNG đụng worklog)
Chỉ cập nhật các section trạng thái trong HUB:
- **📊 Trạng thái**: phase/tiến độ + stack.
- **✅ Đã xong**: tóm tắt từ `completed_tasks`.
- **⛔ Còn lại**: checklist từ `pending_tasks`.
- GIỮ NGUYÊN block Dataview worklog + các link.

## Bước 4 — Report
```
🔄 HUB SYNCED

🗂️  Hub:      [[<hub_note>]]
📊 Updated:  trạng thái · đã-xong · roadmap (<N> pending)
🔗 Worklog list (Dataview) giữ nguyên.
```

## KHÔNG làm
- ❌ Tạo/sửa worklog entry (đó là việc của `/worklog`).
- ❌ Ghi đè block Dataview hoặc phần user tự viết trong hub.
- ❌ Bịa tiến độ — lấy từ `project_state.yaml`.
