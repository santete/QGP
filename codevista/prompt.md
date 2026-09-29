Tiếp quản dự án tại:
C:\Users\nangh\Documents\workplace\ClaudeCode\qms-governance-platform

Làm theo đúng 3 bước dưới. DỪNG sau bước 3, không được tự động code tiếp.

BƯỚC 1 — TÌM HIỂU
Đọc, chưa viết code:
- CLAUDE.md ở thư mục gốc — quy trình làm việc của dự án
- .claude/memory/project_state.yaml — tiến độ, quyết định, gotcha đã ghi
- .claude/memory/task_tracker.yaml — danh sách task, cái nào xong, cái nào chưa
- .claude/memory/schema_snapshot.yaml — schema DB và API
- product-spec/ — tài liệu sản phẩm (PRD, SDD, ADR)
- docs/ai/ — quy ước code, API, DB, test
- apps/api/ và apps/web/ — source backend và frontend

BƯỚC 2 — TRÌNH BÀY
Báo cáo lại, gồm đúng 4 phần:
1. Sản phẩm này làm gì, cho ai dùng, các vai trò và quyền hạn
2. Kiến trúc và luồng nghiệp vụ chính
3. Quy ước code đang áp dụng
4. Tiến độ hiện tại: đã xong những gì, còn lại những gì

Mọi thông tin phải dẫn nguồn cụ thể (tên file, số dòng). Chỗ nào chưa xác minh
được thì ghi rõ "chưa xác minh", không được suy đoán.

BƯỚC 3 — LÊN KẾ HOẠCH
Đề xuất việc cần làm tiếp theo. Kế hoạch phải có:
- Danh sách công việc, sắp theo thứ tự ưu tiên, kèm lý do chọn thứ tự đó
- Với mỗi việc: file nào sẽ sửa, file nào tạo mới, làm gì trong đó
- Cách kiểm chứng việc đó đã xong (test nào, lệnh nào, kết quả mong đợi)
- Rủi ro và ảnh hưởng tới phần đang chạy
Không được ghi chung chung kiểu "TBD", "làm sau", "tương tự phần trên".

DỪNG TẠI ĐÂY.
Gửi báo cáo bước 2 và kế hoạch bước 3 cho tôi duyệt. Chỉ khi tôi duyệt xong mới
được bắt đầu thực thi. Tự ý code trước khi duyệt coi như làm sai.
