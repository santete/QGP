# Nhật ký đánh giá CodeVista — dự án QGP

> Dùng để điền vào sổ phản hồi (sheet `02_Feedback_Log`, dòng `run-10`, dev SangTT).
> Ghi liên tục trong lúc chạy, không nhớ bằng đầu.

**Mốc gốc kho mã:** `396135f` (chốt trước khi giao việc, kho mã lúc đó sạch hoàn toàn)
**Ngày:** 13/09/2026
**Mô hình:** GLM 5.2 (sheet đã ghi sẵn ở dòng run-10)
**Tổng thời gian:** ~50 phút (23:11:16 nhận đề bài 13/09 → 00:00:52 bản ghi cuối 14/09)

### Mốc thời gian (dựng lại từ git + thời gian sửa tập tin)

| Lúc | Chuyện gì |
|---|---|
| 13/09 22:44:24 | Chốt mốc gốc `396135f`, bàn giao |
| 13/09 23:11:16 | CodeVista nhận đề bài (`codevista/prompt.md`), bắt đầu bước 1 |
| 13/09 23:29:10 | Báo cáo bước 2 chốt (sau 3 lượt sửa của bước 1) |
| 13/09 23:49:36 | Kế hoạch bước 3 chốt (sau 2 lượt) |
| 13/09 23:55:09 | Bản ghi 1 `bed957b` — điền PROJECT_MAP |
| 14/09 00:00:12 | Bản ghi 2 `4ec0daf` — màn sửa audience |
| 14/09 00:00:52 | Bản ghi 3 `5d8923e` — đổi phân loại |

> Nguồn số liệu: `git log --date=format` cho 3 bản ghi, `stat` cho thời gian sửa cuối
> của `codevista/prompt.md`, `codevista-review-qgp.md`, `codevista-action-plan.md`.

Chia chặng: bước 1+2 ~18 phút · bước 3 ~20 phút · thực thi ~11 phút.
Đây là thời gian đồng hồ, gồm cả lúc mình soi và lúc user chuyển qua lại.
Thời gian CodeVista thực sự làm ước ~35-40 phút.

---

## Cách giao việc

Chia 3 bước, bắt dừng lại chờ duyệt sau bước 3:
1. Tìm hiểu (đọc, chưa được viết mã)
2. Trình bày lại hiểu biết theo 4 phần
3. Lên kế hoạch, dừng chờ duyệt

---

## Bước 1 — Tìm hiểu

### Lượt 1

Ra tập tin `codevista-review-qgp.md`, 817 dòng, 13 mục.

Làm được:
- Hiểu đúng bản chất sản phẩm, kiến trúc, quy ước — không bịa ra thứ không tồn tại
- Bắt được cả 3 điểm yếu đã gài sẵn: tập tin bản đồ dự án còn trống, số liệu phân loại lỗi thời, còn 1 việc chưa xong
- Số lần chuyển đổi cấu trúc dữ liệu (8) và số quyết định kiến trúc (15) — kiểm lại đúng
- **Không đụng vào một dòng mã nào** — đúng yêu cầu

Sai (3 lỗi, đều là sót chứ không phải bịa):
1. Mục 13 viết phần kho tri thức và phần phản hồi "chưa xây dựng" — sai, cả hai đã xong
   (`apps/web/src/pages/KbPage.tsx`, `FeedbackTriagePage.tsx`, khai báo ở `router.tsx` dòng 46 và 68)
2. Bảng đường dẫn màn hình liệt kê 9 dòng, thực tế 16. Ghi sai `/forbidden`, đúng phải là `/403`
3. Danh sách lớp xử lý nghiệp vụ thiếu 4 tập tin, trong đó có lớp xử lý phản hồi

Không làm đúng đề bài:
- Đề bài yêu cầu dẫn nguồn kèm số dòng — nó chỉ ghi tên tập tin, không có số dòng nào
- Hai con số bài kiểm thử (67 và 43) chép từ sổ ghi nhớ, không tự chạy

### Lượt 2 — sau khi báo 3 lỗi

Sửa đúng:
- Bảng đường dẫn: đủ 16 dòng, có số dòng, `/403` đúng, thêm cả yêu cầu vai trò cho từng màn
- Danh sách lớp xử lý: đủ 19 tập tin, mỗi cái có mô tả đúng việc
- Mục 13: gỡ hẳn 2 đề xuất sai (12 mục còn 11)
- Bắt đầu dẫn số dòng: 20 chỗ, kiểm lại đúng hết

Còn sót:
- Cùng cái sai đó vẫn nằm ở mục 11 (dòng 757-758) và bảng mô-đun (dòng 47)
- → **Sửa đúng chỗ được chỉ tay, không tự truy xem cái sai còn nằm đâu nữa**

### Lượt 3 — sau khi chỉ chỗ sót

Sửa sạch:
- Mục 11: gỡ 2 dòng sai, thêm ghi chú đính chính có dẫn nguồn
- Bảng mô-đun dòng 47: tự sửa luôn dù không được nhắc — **bắt đầu tự truy**
- Kiểm lại toàn bộ 6 chỗ dẫn số dòng trong tài liệu: đúng cả 6

**Kết luận bước 1: Đạt sau 3 lượt.** Sai vì đọc lướt, không phải bịa. Bị bắt thì sửa đúng,
không cãi, không bịa thêm để chống chế.

---

## Bước 2 — Trình bày

Viết lại `codevista-review-qgp.md`, **ghi đè bản cũ**, 843 dòng còn 405, chia đúng 4 phần
theo yêu cầu.

Làm tốt:
- Trả đủ 4 phần, không thiếu không lạc đề
- Dẫn nguồn dày đặc. Kiểm lại khoảng 20 chỗ, **đúng hết, không lệch một dòng**:
  - Số dòng tập tin: đường dẫn 237, thiết lập đăng nhập 104, xử lý phản hồi 91,
    so sánh khác biệt 66, phân vùng nhật ký 38, chuyển trạng thái 32
  - Số dòng màn hình: 112, 115, 136, 233, 149, 170, 90
  - `QgpAuth.cs` dòng 6-11 (6 vai trò) và 32-36 (5 nhóm quyền)
  - `VersionStatus.cs` dòng 8-18, `CLAUDE.md` 385 dòng, `task_tracker.yaml` dòng 117
- **Tự thêm cột ghi mức độ chắc chắn** của từng thông tin: đọc trực tiếp / suy từ tập tin khác /
  lấy từ sổ ghi nhớ. Không được yêu cầu mà tự làm. Đây là điểm sáng nhất.

Chưa khéo:
- Ghi đè bản cũ, không giữ bản sao. Mất: bảng 16 bảng dữ liệu kèm ràng buộc,
  18 quyết định kiến trúc, danh sách lưu ý từ 25 rút còn 13
- Tập tin chưa vào kho mã nên không lấy lại được
- Tiêu đề mục 4.3 ghi 25 lưu ý nhưng chỉ liệt kê 13 (có ghi chú, không phải nói dối)
- Vẫn chưa tự chạy bài kiểm thử

**Kết luận bước 2: Đạt.** Đã bỏ được thói đoán mò.

---

## Bước 3 — Lên kế hoạch

### Lượt 1

Ra tập tin mới `codevista-action-plan.md`, 178 dòng, 6 việc. **Lần này không ghi đè
bản báo cáo cũ** — đã rút kinh nghiệm từ bước 2.

Làm đúng:
- Đủ 4 thứ cho mỗi việc: lý do xếp hạng, tập tin đụng vào, cách kiểm chứng, rủi ro
- Tự nhận ra 3 việc chạm vùng cấm và ghi rõ phải xin phép (môi trường thật, cài thư viện
  mới, đổi cấu trúc dữ liệu)
- Khuyến nghị làm việc 1 và 3 trước — đúng
- Việc số 1 có hẳn danh sách **tập tin KHÔNG được đụng** — chủ động phòng làm dư
- Bắt được cái bẫy thật: lệnh lưu audience là **thay thế toàn bộ** danh sách, giao diện
  phải cảnh báo. Chỗ này phải đọc mã mới thấy, không đoán ra được
- Dẫn nguồn đúng: `DocumentService.cs:19,22` · `V1Endpoints.cs:188-194` ·
  `Dtos.cs:251-252` và `:198-202` · `roles.ts:33` · `DocumentPage.tsx` 30 dòng

Sai — **và cả hai đều nằm trong việc số 1, tức việc nó sắp làm**:

1. **BỊA TẬP TIN.** Ghi làm theo tiền lệ `useDocument.ts` và `useDocumentList.ts`.
   Cái thứ hai **KHÔNG TỒN TẠI**. Thư mục `apps/web/src/features/documents/` chỉ có
   `useDocument.ts`, `useDocumentSearch.ts`, `useVersionHistory.ts`.
   → **Đây là lần ĐẦU TIÊN nó bịa.** Ba lượt trước sai đều là sót (có thật mà không
   thấy), lần này ngược lại (không có mà viết ra như có).
   → **Cột "Có bịa API / hàm không có thật?" đổi từ `Không` sang `Có vài chỗ`.**

2. **Số dòng sai, và tự biết là đoán.** Ghi `getDocAudience` khoảng dòng 440,
   `setDocAudience` khoảng dòng 448 trong `client.ts` — thực tế 469 và 475.
   Dòng 440 là hàm xoá thẻ, hoàn toàn khác. Chữ "khoảng" là dấu hiệu nó không mở ra xem.
   → Bước lùi so với bước 2 (lúc đó dò 20 chỗ không lệch dòng nào).

Sai nhỏ hơn:
3. **Thiếu việc điền `docs/ai/PROJECT_MAP.md`** — chính nó xếp là đề xuất số 1 ở báo cáo
   bước 2, sang bước 3 biến mất. Tự mâu thuẫn sau đúng một bước.
4. Ghi phân loại mức A là "≤12k dòng" — thực ra mức A là ≤10k, 12k là ngưỡng xem xét lại.
5. Kiểm thử ghi "43+1" và viết trong lúc làm — quy trình dự án bắt buộc viết kiểm thử
   TRƯỚC. Một bài cho màn hình có 3 nhánh hành xử là quá mỏng.

**Kết luận bước 3 lượt 1: CHƯA duyệt.** Cấu trúc đạt nhưng 2 lỗi nặng nằm đúng trong
việc sắp thực thi. Đã gửi lại 4 điểm yêu cầu sửa.

### Lượt 2 — sau khi báo 4 lỗi

Kế hoạch 178 → 229 dòng. Cả 4 đều sửa xong, kiểm lại:

1. **Tập tin bịa đã gỡ** — và thay bằng 2 tập tin CÓ THẬT kèm khoảng dòng chính xác:
   `useDocument.ts:33-47` và `useDocumentSearch.ts:14-29`. Mở ra dò: đúng là 2 đoạn đó
   chứa khuôn mẫu huỷ tác vụ mà nó nói tới. **Lần này mở tập tin ra đọc thật rồi mới dẫn.**
2. **Số dòng sửa đúng** 469 / 475, bỏ chữ "khoảng".
3. **Bổ sung việc điền PROJECT_MAP**, xếp lên hẳn **ưu tiên 0** (trước cả b1c1) — lập luận
   hợp lý hơn cách xếp cũ. Số liệu dẫn đúng: tập tin 119 dòng, `CLAUDE.md:15`.
   Cách kiểm chứng gọn: đếm placeholder `<vd:` phải = 0 (hiện đang là 7).
4. **Kiểm thử viết lại theo đúng TDD**: viết test → chạy cho FAIL → implement → chạy cho
   PASS. 3 trường hợp cụ thể (admin thấy / người thường không thấy / bấm lưu gửi đúng
   hình dạng dữ liệu). Dẫn `CLAUDE.md:112-122` — kiểm lại đúng.

Còn sót (không nằm trong danh sách bắt sửa nên không trách):
- Vẫn ghi mức A là "≤12k dòng" (đúng ra ≤10k, 12k là ngưỡng xem xét lại)
- Dẫn vùng cấm "đổi cấu trúc dữ liệu" vào `CLAUDE.md:248`, thực ra dòng 247 (248 là đổi
  khoá bí mật). Lệch 1 dòng, không ảnh hưởng kết luận.

**Kết luận bước 3: ĐÃ DUYỆT.** Kế hoạch chạy được.

⚠️ Lưu ý khi giao thực thi: kế hoạch có **7 việc (0-6)**. Chỉ cho làm **0, 1, 3**.
Việc 2 (môi trường thật), 4 (dịch vụ mới + thư viện mới), 5 (thư viện mới),
6 (đổi cấu trúc dữ liệu) đều là vùng cấm — không để nó tự quyết.

---

## Bước 4 — Thực thi (ưu tiên 0, 1, 3)

3 bản ghi, mỗi việc 1 cái, định dạng Conventional Commits đúng, mô tả viết kỹ:
- `bed957b` docs(ai): điền PROJECT_MAP
- `4ec0daf` feat(audience): bảng sửa audience per-document (b1c1)
- `5d8923e` chore(classify): đổi sang pattern B

7 tập tin bị đụng — **khớp hoàn toàn với danh sách khai trong kế hoạch, không lấn ra ngoài**.

### Kiểm chứng độc lập (tự chạy hết)

| Hạng mục | Mốc gốc | Sau khi làm | |
|---|---|---|---|
| Kiểm thử máy chủ | 67 | **67 đậu / 0 hỏng** | không regression |
| Kiểm thử giao diện | 43 | **46 đậu / 0 hỏng** | đúng +3 như kế hoạch |
| Kiểm kiểu dữ liệu | sạch | **sạch** (mã thoát 0) | |
| Dựng bản giao diện | được | **được** (395KB / nén 118KB) | |

### Làm tốt

- **Không bịa một symbol nào.** Soi hết mọi import trong mã mới: `ROLE_LABEL` (roles.ts:23),
  `Card`, `Button` (có thật prop `loading`/`size`), `useToast` (Toast.tsx:58) — đều có thật,
  chữ ký dùng đúng. Khác hẳn bước 3.
- **Phân quyền đúng:** `hasAnyRole(ADMIN_ROLES)` gate ở DocumentPage → người thường không
  render bảng nên **cũng không gọi API**, không phải vẽ ra rồi giấu.
- **Làm thật cái rủi ro nó tự phát hiện:** cảnh báo "lưu sẽ thay thế toàn bộ danh sách" có
  trong i18n + hiển thị màu cảnh báo.
- Chữ tiếng Việt để trong `i18n/strings.ts`, không nhét thẳng vào component.
- Kiểm thử viết thật, dùng `vi.hoisted` + mock đúng cách, 3 trường hợp như cam kết.
- **Trung thực trong mô tả bản ghi:** chỉ khai "FE 46/46, typecheck 0 errors" —
  KHÔNG khai số phía máy chủ vì nó không chạy. Không bịa số cho đủ bộ.

### Sai — 2 chỗ phải sửa

1. **NẶNG — viết câu sai vào tập tin mọi agent đọc đầu tiên.**
   `docs/ai/PROJECT_MAP.md:102` ghi chạy kiểm thử máy chủ "không DB thật".
   **Đã chứng minh sai:** không bật Postgres → **34/67 hỏng** (toàn lỗi Npgsql không kết
   nối). Bật lên → 67/67. Nó chép từ `project_state.yaml` decision #9 (ghi chú cũ, có lẽ
   đúng khi dự án còn ít test) mà không chạy thử.
   → Đúng cái tật lặp lại suốt từ bước 1: **tin tài liệu hơn tin kết quả chạy**.
   `PROJECT_MAP.md:110` cũng ghi FE 43 bài — chính nó vừa nâng lên 46.

2. **Cả 3 bản ghi thiếu dấu `[AI]`** cuối dòng tiêu đề.
   Vi phạm `R-COMMIT-002-AI` (`01_MR_Compliance.md:94`, severity REQUIRED, nhắc lại ở
   `00_INDEX.md:180` và cross-cutting #4 dòng 240). User đã dặn rõ "commit theo đúng
   quy ước của dự án".

Vụn:
- LOC 57314 nó đo, mình đo lại theo đúng bộ lọc ra 61-65k tuỳ cách lọc. Lệch vài nghìn
  nhưng **kết luận Pattern B đúng ở mọi cách đo**.
- Thêm chuỗi `cancel: 'Huỷ'` vào i18n nhưng giao diện không có nút huỷ. Thừa, vô hại.

**Kết luận bước 4: ĐẠT.** Xong cả 3 việc, mã chạy được ngay, không làm hỏng gì, không bịa
trong mã. 2 chỗ sửa đều nhanh. Đã gửi yêu cầu sửa.

---

## Kiểm chứng độc lập (tự chạy, không tin số nó khai)

| Lúc nào | Kiểm gì | Kết quả |
|---|---|---|
| Sau bước 1 lượt 1 | Bài kiểm thử phía giao diện | 43/43 đậu — khớp số nó khai |
| Suốt 3 lượt | Kho mã có bị đụng không | Không. Chỉ thêm 2 tập tin mới |
| Bước 2 | ~20 chỗ dẫn số dòng | Đúng hết |

Chưa kiểm: bài kiểm thử phía máy chủ (67) — cần bật cơ sở dữ liệu, để tới lúc có mã mới chạy.

---

## Nháp dòng sổ — đủ 22 cột, điền đúng đáp án cho sẵn

> Ô nền vàng = tự điền. Ô nền xám = sổ tự tính, KHÔNG gõ đè (`Có người review?`, `Kết luận`).

| # | Cột | Giá trị dự kiến | Ghi chú |
|---|---|---|---|
| 1 | Mã | `run-10` | dòng có sẵn của SangTT |
| 2 | Ngày | `13/09/2026` | |
| 3 | Dev | `SangTT` | |
| 4 | Người review PR | _trống_ | chưa có người thứ hai xem |
| 5 | Có người review? | — | **sổ tự tính** từ cột 4 |
| 6 | Model | `GLM 5.2` (sheet đã có sẵn) | |
| 7 | Kịch bản | `GF-3 · Phát triển tính năng mới (từ dự án legacy)` | đã có sẵn ở run-10 |
| 8 | Nhóm | `Greenfield` | đi kèm GF-3 |
| 9 | Repo / Module | `qms-governance-platform` (apps/web + docs/ai) | |
| 10 | Việc cụ thể là gì | Giao tiếp quản dự án: đọc hiểu codebase, trình bày lại, lên kế hoạch, rồi làm nốt việc còn dở (màn sửa audience) | ghi kèm tóm tắt đề bài 3 bước |
| 11 | Agent làm được tới đâu? | `Xong hết` | chọn: Xong hết · Xong một phần · Không dùng được |
| 12 | Code chạy được ngay? | `Chạy được ngay` | chọn: Chạy được ngay · Phải sửa mới chạy · Không áp dụng |
| 13 | Có làm hỏng chỗ đang chạy? | `Không` — đã xác nhận BE 67/67, FE 46/46 | chọn: Không · Hỏng nhẹ · Hỏng nặng |
| 14 | Bạn phải sửa lại bao nhiêu? | `Sửa ít` | chọn: Gần như không · Sửa ít · Sửa nhiều · Viết lại từ đầu |
| 15 | So với tự làm tay? | **chỉ dev trả lời được** | chọn: Nhanh hơn nhiều · Nhanh hơn một chút · Ngang nhau · Chậm hơn tự làm |
| 16 | Có bịa API / hàm không có thật? | `Có vài chỗ` | ĐỔI ở bước 3: bịa `useDocumentList.ts` (không tồn tại) + đoán sai số dòng `client.ts` |
| 17 | Kết quả cuối cùng | _chờ_ | chọn: Đã merge · Đang review · Bỏ không dùng |
| 18 | Hài lòng (1–5) | `4` (đề xuất — dev chốt) | không cho 5 vì 3 lỗi đáng kể đều do người soi bắt, không phải nó tự phát hiện |
| 19 | Thời gian dùng agent (phút) | `50` | không bắt buộc |
| 20 | Kết luận | — | **sổ tự tính** |
| 21 | Link PR / commit | `396135f` (gốc) → `bed957b`, `4ec0daf`, `5d8923e` | |
| 22 | Góp ý cho đội CodeVista | xem mục dưới | phần sổ ghi là giá trị nhất |

### Luật tự tính cột Kết luận (để biết ô nào kéo tụt kết quả)

- **Thất bại** ← "Không dùng được" HOẶC "Hỏng nặng" HOẶC "Bỏ không dùng"
- **Có điều kiện** ← "Xong một phần" HOẶC "Hỏng nhẹ" HOẶC "Phải sửa mới chạy" HOẶC "Sửa nhiều / Viết lại từ đầu"
- **Đạt** ← còn lại

Lưu ý: **"Sửa ít" KHÔNG kéo xuống Có điều kiện.** Ba lỗi ở bước 1 quy về "Sửa ít" thì
dòng này vẫn giữ được mức Đạt. Nhưng nếu bước 3 trở đi mà mã phải sửa mới chạy, hoặc
làm hỏng chỗ đang chạy, thì tụt ngay xuống Có điều kiện.

### Tiêu chí riêng của kịch bản GF-3 (sheet 01 ghi rõ, phải theo dõi)

> "Có bám theo cách viết sẵn có của repo không; đề bài mơ hồ thì nó hỏi lại hay tự đoán."

- **Bám theo cách viết sẵn có:** _chờ bước 3, lúc có mã mới đánh giá được_
- **Hỏi lại hay tự đoán:** tới hết bước 2, **chưa hỏi lại câu nào**. Chỗ không chắc thì
  tự suy rồi viết ra, bị bắt mới sửa. Ví dụ rõ nhất: đề bài không nói báo cáo nên giữ
  bản cũ hay ghi đè — nó tự quyết ghi đè, mất 438 dòng nội dung, không hỏi một tiếng.

## Góp ý gom được cho đội CodeVista

1. Khi được báo lỗi, nó chỉ sửa đúng dòng được chỉ tay, không tự truy xem cái sai đó còn
   nằm ở đâu khác trong cùng tập tin. Phải nhắc lần hai mới tự truy.
2. Ghi đè tập tin cũ mà không hỏi và không giữ bản sao. Nên cảnh báo trước khi ghi đè
   tập tin nó đã tạo ở lượt trước, nhất là khi tập tin đó rút ngắn đi nhiều.
3. Mặc định nó chép số liệu (số bài kiểm thử...) từ tài liệu sẵn có mà không tự chạy để
   xác nhận. Nên phân biệt rõ "số tôi đọc được" và "số tôi tự đo".
4. Gặp chỗ đề bài không nói rõ thì nó tự quyết chứ không hỏi lại. Suốt 3 lượt chưa hỏi
   một câu nào. Đây đúng là điểm sheet kịch bản GF-3 dặn phải để ý.
5. Điểm tốt nên giữ: tự khai mức độ chắc chắn của từng thông tin trong báo cáo
   (đọc trực tiếp / suy ra / lấy từ tài liệu sẵn có).
