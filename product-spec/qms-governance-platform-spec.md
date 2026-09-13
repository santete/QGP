---
title: "Nền tảng Quản trị Quy trình & Chất lượng — QMS Governance Portal"
doc_id: PLAT-SPEC-QMS
version: "0.1"                       # Draft for review
status: DRAFT
owner: "QA / Process Governance (PhucDN7)"
audience: "QA, PMO, PM, BA, SA, PO, TL, DevOps/SRE, Ops PIC — nội bộ FPT ISC"
date_created: 2026-07-10
depends_on:
  - "AI-SDLC 8-Step Framework"
  - "Artifact Catalog (16 artifacts)"
  - "Quality Gate 1 & 2 (Excel)"
  - "Naming Convention — Telco ISP Microservices"
impacts:
  - "Onboarding process (mọi role mới)"
  - "Core Telco → Delivery Handover framework"
  - "Windows→Ubuntu migration (làm nơi host tài liệu hướng dẫn)"
format_rationale: "Developer/system-facing governance doc → Markdown (AI-agent readable)"
---

# Nền tảng Quản trị Quy trình & Chất lượng (QMS Governance Portal)

> **Tên gọi là placeholder** — cần chốt tên chính thức + viết tắt (gợi ý: **QGP** — Quality Governance Portal, hoặc **QMS Portal**). Toàn bộ tài liệu dùng "Portal" cho ngắn.

---

## 1. Mục tiêu & Vấn đề giải quyết

Hiện trạng tài liệu quy trình QA nằm rải rác (Excel, HTML, Markdown, ổ chia sẻ) → không ai biết bản nào **đang có hiệu lực**, người mới không biết đọc từ đâu, QA không có số liệu để báo cáo, feedback đi qua chat rồi trôi. Portal giải quyết đúng 6 nhu cầu:

| # | Nhu cầu | Phân hệ đáp ứng |
|---|---------|-----------------|
| 1 | Một nguồn sự thật duy nhất cho tài liệu quy trình, kiểm soát chặt version / ngày ban hành / ngày áp dụng | **DOC** |
| 2 | Wiki cho người mới + Knowledge cho người cũ tra cứu | **KB** |
| 3 | Onboarding tự phục vụ, có theo dõi tiến độ & xác nhận đã đọc | **ONB** |
| 4 | Báo cáo quản trị: số tài liệu ban hành, lượt truy cập, lượt tra cứu | **RPT** |
| 5 | Kênh feedback chính thức về phòng QA | **FBK** |
| 6 | Chatbot AI hỗ trợ tìm & tra cứu quy trình | **BOT** |

**Không phải mục tiêu (Out of Scope):** xem [§12](#12-ngoài-phạm-vi-out-of-scope).

---

## 2. Nguyên tắc thiết kế (Design Principles)

1. **Single Source of Truth** — Nội dung chuẩn (normative) chỉ tồn tại 1 nơi (DOC). Mọi nơi khác (KB, BOT, Onboarding) chỉ *tham chiếu*, không sao chép.
2. **Approved ≠ Effective** — Một tài liệu có thể đã duyệt/ban hành nhưng chưa tới ngày áp dụng. Portal phải phân biệt rõ, không đánh đồng.
3. **Low-friction** — Người đọc/feedback không phải điền form dài. Đóng góp phải dễ hơn im lặng.
4. **Traceability by default** — Mọi tài liệu có `depends_on` / `impacts`; mọi thay đổi có audit trail.
5. **AI-first readability** — Tài liệu lưu ở dạng structured (Markdown + frontmatter) để BOT index chính xác, không phải parse PDF mờ.
6. **Governed AI** — Chatbot chỉ trả lời từ tài liệu `Effective`, luôn trích nguồn, được phép nói "không chắc".

---

## 3. Bản đồ phân hệ (Module Map)

| Mã | Phân hệ | Vai trò | Phase |
|----|---------|---------|-------|
| **DOC** | Quản trị Tài liệu & Version | Lõi normative — vòng đời, version, phê duyệt, ngày hiệu lực | P1 |
| **KB** | Wiki & Knowledge Base | Tầng giải thích, tra cứu, cross-link về DOC | P1 |
| **ONB** | Onboarding | Learning path theo role, checklist, xác nhận đã đọc | P2 |
| **RPT** | Báo cáo & Thống kê Quản trị | Metrics tài liệu, truy cập, tra cứu, compliance | P2 |
| **FBK** | Feedback & Cải tiến | Kênh phản hồi + đề xuất cải tiến (CAPA-lite) về QA | P1 |
| **BOT** | AI Assistant (RAG) | Hỏi–đáp & tìm kiếm ngữ nghĩa trên corpus tài liệu | P3 |
| **ADM** | Quản trị Hệ thống & Phân quyền | RBAC, taxonomy, notification, audit log | P1 |

> **Phase P1** = MVP dùng được ngay (DOC + KB + FBK + ADM). **P2** = vận hành & đo lường. **P3** = AI. Không làm BOT trước khi corpus đủ sạch — rác vào, rác ra.

---

## 4. DOC — Quản trị Tài liệu & Version *(lõi)*

**Mục đích:** Là nơi duy nhất chứa tài liệu chuẩn có hiệu lực, kiểm soát chặt vòng đời và phiên bản.

### 4.1 Tính năng

| ID | Tính năng | MoSCoW | Phase | Ghi chú |
|----|-----------|:------:|:-----:|---------|
| DOC-F-01 | Tạo/sửa tài liệu với metadata bắt buộc (xem §4.3) | M | P1 | Frontmatter YAML |
| DOC-F-02 | Version hoá major.minor tự động khi publish | M | P1 | Quy tắc §4.4 |
| DOC-F-03 | Ghi nhận **ngày ban hành** & **ngày áp dụng** riêng biệt | M | P1 | Effective date có thể ở tương lai |
| DOC-F-04 | Workflow phê duyệt: Draft → Review → Approved → Published | M | P1 | RACI theo loại tài liệu |
| DOC-F-05 | Vòng đời trạng thái đầy đủ + tự chuyển Effective theo ngày | M | P1 | State machine §4.2 |
| DOC-F-06 | Lịch sử phiên bản + diff giữa 2 version | M | P1 | Xem "cái gì đã đổi" |
| DOC-F-07 | Quan hệ tài liệu: `supersedes` / `superseded_by`, `depends_on` / `impacts` | S | P1 | Cảnh báo tác động khi sửa |
| DOC-F-08 | Chu kỳ review (next_review_date) + nhắc trước hạn | M | P2 | Chống tài liệu "chết già" |
| DOC-F-09 | Xác nhận đã đọc (acknowledgement) khi tài liệu là bắt buộc | S | P2 | Feed vào ONB & RPT |
| DOC-F-10 | Phân loại (classification) 3 trục kế thừa Handover: GF/BF · Tier · Loại tài liệu | S | P1 | Tái dùng phân loại sẵn có |
| DOC-F-11 | Export PDF có watermark version + trạng thái | C | P2 | Cho audit/khách hàng |
| DOC-F-12 | Cảnh báo khi truy cập bản **không phải Effective** | M | P1 | Tránh dùng nhầm bản cũ/nháp |

### 4.2 Vòng đời tài liệu (State Machine)

```
Draft ──submit──▶ In Review ──approve──▶ Approved ──publish──▶ Published
                     │ reject                                      │
                     ▼                                    (đến ngày áp dụng)
                   Draft                                           ▼
                                                              Effective ◀──┐
                                                                  │        │ minor revision
                                              start revision      ▼        │ (ban hành lại)
                                                          Under Revision ───┘
                                                                  │ major change → phê duyệt lại
                                                                  ▼
Effective (bản mới) ──supersedes──▶ Superseded ──retention hết──▶ Retired
```

**Quy tắc bất di bất dịch:**
- `Approved` ≠ `Effective`. Publish xong vẫn có thể *chưa* có hiệu lực nếu `effective_date` ở tương lai → Portal hiển thị badge "Sắp áp dụng từ dd/mm/yyyy".
- Chỉ **1 bản `Effective`** cho mỗi `doc_id` tại một thời điểm. Bản mới lên Effective → bản cũ tự động `Superseded`.
- `Draft` / `In Review` **không** được BOT index, **không** hiện trên KB công khai.

### 4.3 Metadata tài liệu (Document Object)

| Trường | Bắt buộc | Ví dụ | Ghi chú |
|--------|:--------:|-------|---------|
| `doc_id` | ✓ | `QA-PROC-005` | Namespaced, không đổi suốt vòng đời |
| `title` | ✓ | "Quy trình Quality Gate 1" | |
| `type` | ✓ | Policy / Process / Procedure / Work Instruction / Template / Checklist / Standard | |
| `version` | ✓ | `2.1` | major.minor |
| `status` | ✓ | Effective | Theo state machine |
| `owner` | ✓ | QA Lead | RACI = Accountable |
| `issue_date` | ✓ | 2026-07-01 | **Ngày ban hành** |
| `effective_date` | ✓ | 2026-07-15 | **Ngày áp dụng** |
| `next_review_date` | ✓ | 2027-07-01 | Chu kỳ review |
| `classification` | S | BF · Tier-1 · Process | 3 trục kế thừa Handover |
| `supersedes` / `superseded_by` | – | `QA-PROC-005 v2.0` | |
| `depends_on` / `impacts` | – | `[QA-STD-002]` | Traceability |
| `mandatory_ack` | – | true | Có yêu cầu xác nhận đã đọc? |
| `tags` | – | `[quality-gate, deploy]` | Cho tìm kiếm & KB |

### 4.4 Quy tắc version

- **Major (x.0)** = thay đổi *normative* (đổi quy tắc, bước, tiêu chí) → **phải phê duyệt lại** + reset acknowledgement (người dùng phải đọc & xác nhận lại).
- **Minor (x.y)** = sửa editorial (chính tả, làm rõ, format) → owner tự phát hành, **không** reset acknowledgement.
- Mọi lần publish **bắt buộc** có `change_summary` (1–3 dòng "đổi cái gì, vì sao").

---

## 5. KB — Wiki & Knowledge Base

**Mục đích:** Tầng *giải thích* — giúp người mới hiểu bối cảnh, người cũ tra cứu nhanh. **Không chứa nội dung chuẩn**, chỉ diễn giải & link về DOC.

| ID | Tính năng | MoSCoW | Phase | Ghi chú |
|----|-----------|:------:|:-----:|---------|
| KB-F-01 | Trang wiki phân cấp theo chủ đề (taxonomy) | M | P1 | Cây chủ đề do ADM quản |
| KB-F-02 | Nhúng/tham chiếu tài liệu DOC (transclude) — luôn hiện version + trạng thái | M | P1 | Chống lệch nội dung |
| KB-F-03 | Tìm kiếm full-text + filter theo tag / loại / phân hệ | M | P1 | |
| KB-F-04 | Trang "Bắt đầu từ đâu" theo role (điểm vào cho người mới) | S | P1 | Nối sang ONB |
| KB-F-05 | FAQ có cấu trúc (câu hỏi → câu trả lời → link DOC gốc) | S | P2 | Nguồn dữ liệu tốt cho BOT |
| KB-F-06 | Cảnh báo "trang tham chiếu tài liệu đã Superseded" | M | P1 | Tự phát hiện khi DOC đổi |
| KB-F-07 | Glossary thuật ngữ Telco/QA (customer-svc, Quality Gate, Hypercare…) | C | P2 | |

> **Ranh giới cứng:** nếu một trang KB bắt đầu chứa "quy tắc phải làm" thay vì "giải thích cách làm" → nội dung đó phải chuyển thành tài liệu DOC. KB không được là nơi đẻ ra luật lệ ngầm.

---

## 6. ONB — Onboarding

**Mục đích:** Người mới lên Portal là tra cứu được ngay, đi theo lộ trình rõ ràng, QA kiểm soát được "ai đã đọc gì".

| ID | Tính năng | MoSCoW | Phase | Ghi chú |
|----|-----------|:------:|:-----:|---------|
| ONB-F-01 | Learning path theo role (PM/BA/SA/QA/DevOps/OP…) | M | P2 | Chuỗi tài liệu + KB xếp thứ tự |
| ONB-F-02 | Checklist onboarding có theo dõi tiến độ % | M | P2 | Low-friction, tick là xong |
| ONB-F-03 | Yêu cầu đọc & xác nhận tài liệu bắt buộc (nối DOC-F-09) | S | P2 | Sinh bằng chứng compliance |
| ONB-F-04 | Trang tổng quan cá nhân "việc cần đọc / đã đọc" | S | P2 | |
| ONB-F-05 | Quiz kiểm tra hiểu (tùy chọn, không bắt buộc) | C | P3 | Chỉ làm nếu QA thấy cần |

> **Cảnh báo scope:** đừng biến ONB thành LMS đầy đủ (chấm điểm, chứng chỉ, học liệu video). Giữ đúng "lộ trình đọc + checklist + xác nhận". Cần LMS thật thì đó là dự án khác.

---

## 7. RPT — Báo cáo & Thống kê Quản trị

**Mục đích:** QA có số liệu để báo cáo lãnh đạo & tự đánh giá sức khoẻ hệ thống tài liệu.

| ID | Nhóm chỉ số | Chỉ số cụ thể | MoSCoW | Phase |
|----|-------------|---------------|:------:|:-----:|
| RPT-F-01 | Tài liệu ban hành | Tổng số, theo trạng thái, theo loại, theo phòng ban, số ban hành mới/tháng | M | P2 |
| RPT-F-02 | Sức khoẻ tài liệu | Số tài liệu **quá hạn review**, sắp tới hạn, tỷ lệ có `change_summary` | M | P2 |
| RPT-F-03 | Truy cập & tra cứu | Lượt truy cập, top tài liệu được xem, từ khoá tìm nhiều, tài liệu 0 lượt xem (nghi ngờ thừa) | M | P2 |
| RPT-F-04 | Compliance | % acknowledgement theo tài liệu bắt buộc, ai chưa đọc | S | P2 |
| RPT-F-05 | Feedback | Số feedback theo trạng thái, thời gian phản hồi trung bình của QA | S | P2 |
| RPT-F-06 | Export báo cáo định kỳ (Excel/PDF) | Snapshot hàng tháng | C | P3 |

**Nguyên tắc tách bạch (theo pattern của mày):** *Dashboard* và *dữ liệu thô* nằm ở màn/sheet khác nhau khi dashboard vượt ~10 dòng.

> **Cờ về quyền riêng tư:** metric truy cập/tra cứu mặc định **tổng hợp (aggregate)**, không lộ hành vi từng cá nhân. Chỉ theo dõi ở cấp cá nhân đúng phần *compliance acknowledgement* (RPT-F-04) — vì đó là nghĩa vụ, không phải giám sát. Cần chốt với mày điểm này.

---

## 8. FBK — Feedback & Cải tiến

**Mục đích:** Biến feedback trôi nổi thành luồng chính thức, có vòng đời, về đúng phòng QA.

| ID | Tính năng | MoSCoW | Phase | Ghi chú |
|----|-----------|:------:|:-----:|---------|
| FBK-F-01 | Nút feedback ngay trên từng tài liệu/trang (in-context) | M | P1 | Đúng ngữ cảnh, ít ma sát |
| FBK-F-02 | Loại feedback **chọn sẵn** (dropdown): Lỗi nội dung / Khó hiểu / Đề xuất cải tiến / Câu hỏi | M | P1 | Pre-categorized > free-text |
| FBK-F-03 | Vòng đời feedback: New → Triaged → In Progress → Resolved / Rejected | S | P1 | QA làm chủ triage |
| FBK-F-04 | Tự đính kèm ngữ cảnh (doc_id, version, url) vào feedback | M | P1 | Không bắt người dùng gõ lại |
| FBK-F-05 | Đề xuất cải tiến → có thể chuyển thành yêu cầu sửa tài liệu (CAPA-lite) | C | P2 | Nối sang DOC |
| FBK-F-06 | Thông báo cho người gửi khi feedback được xử lý | S | P2 | Đóng vòng phản hồi |

> **Test "tao có tự điền không?":** một feedback tối thiểu = 1 dropdown + 1 ô mô tả tùy chọn. Không bắt điền tên dự án, mức độ, mức ưu tiên… những cái QA tự gán được khi triage.

---

## 9. BOT — AI Assistant (RAG Chatbot)

**Mục đích:** Trả lời câu hỏi quy trình & dẫn người dùng tới đúng tài liệu — **có kiểm soát**.

| ID | Tính năng | MoSCoW | Phase | Ghi chú |
|----|-----------|:------:|:-----:|---------|
| BOT-F-01 | Hỏi–đáp ngôn ngữ tự nhiên (VN/EN) trên corpus tài liệu | M | P3 | RAG |
| BOT-F-02 | **Chỉ index tài liệu `Effective`** (loại Draft/Superseded/Retired) | M | P3 | Nguyên tắc quản trị |
| BOT-F-03 | Mọi câu trả lời **trích nguồn**: doc_id + version + ngày áp dụng + link | M | P3 | Không citation = không trả lời |
| BOT-F-04 | Biết nói "chưa có trong tài liệu / không chắc" thay vì bịa | M | P3 | Guardrail chống hallucination |
| BOT-F-05 | Re-index tự động khi tài liệu đổi trạng thái | M | P3 | Chống trả lời theo bản cũ |
| BOT-F-06 | Log câu hỏi không trả lời được → gợi ý tài liệu/FAQ cần bổ sung | S | P3 | Feed ngược vào KB/DOC |
| BOT-F-07 | Đề xuất chuyển câu hỏi thành feedback nếu phát hiện lỗi tài liệu | C | P3 | Nối FBK |

> **Điều kiện tiên quyết:** BOT chỉ đáng làm khi DOC + KB đã sạch và ổn định (cuối P2). Làm sớm = dạy chatbot nói bậy bằng dữ liệu rác.

---

## 10. ADM — Quản trị Hệ thống & Phân quyền

| ID | Tính năng | MoSCoW | Phase |
|----|-----------|:------:|:-----:|
| ADM-F-01 | RBAC theo vai trò (bảng §10.1) | M | P1 |
| ADM-F-02 | Quản lý taxonomy / tag / loại tài liệu | M | P1 |
| ADM-F-03 | Audit log toàn hệ thống (ai làm gì, khi nào) | M | P1 |
| ADM-F-04 | Notification & subscription (theo dõi tài liệu/chủ đề) | S | P2 |
| ADM-F-05 | SSO tích hợp tài khoản nội bộ | S | P1 |
| ADM-F-06 | Cấu hình RACI phê duyệt theo loại tài liệu | S | P2 |

### 10.1 Ma trận phân quyền (rút gọn)

| Vai trò | Đọc | Đóng góp KB | Soạn/sửa DOC | Duyệt DOC | Quản trị hệ thống |
|---------|:---:|:-----------:|:------------:|:---------:|:-----------------:|
| Reader (mọi nhân viên) | ✓ | – | – | – | – |
| Contributor (PM/BA/SA/TL/DO/OP) | ✓ | ✓ | – | – | – |
| Document Owner/Author | ✓ | ✓ | ✓ | – | – |
| Approver (Trưởng phòng/QA Lead) | ✓ | ✓ | ✓ | ✓ | – |
| QA Governance Lead | ✓ | ✓ | ✓ | ✓ | ✓ (nghiệp vụ) |
| System Admin | ✓ | ✓ | – | – | ✓ (kỹ thuật) |

---

## 11. Yêu cầu phi chức năng (NFR)

| ID | Loại | Yêu cầu |
|----|------|---------|
| NFR-01 | Availability | Nội bộ, giờ hành chính; target ≥ 99% |
| NFR-02 | Search latency | Tìm kiếm trả kết quả < 1s với corpus vài nghìn tài liệu |
| NFR-03 | Audit & Retention | Audit log giữ ≥ 2 năm; tài liệu Retired giữ theo policy compliance |
| NFR-04 | Security | SSO, RBAC, HTTPS, phân quyền tới cấp tài liệu |
| NFR-05 | Data integrity | Version bất biến — bản đã publish không sửa được, chỉ ra bản mới |
| NFR-06 | Portability | Tài liệu export được ở Markdown gốc (tránh vendor lock-in) |
| NFR-07 | Observability | OpenTelemetry cho tracing/metric hệ thống (chuẩn nội bộ) |
| NFR-08 | Accessibility | UI tiếng Việt, responsive, dùng được trên mobile |

---

## 12. Ngoài phạm vi (Out of Scope)

Nêu rõ để chống scope drift:

- ❌ Không phải LMS (không quản lý khoá học, video, chứng chỉ, điểm số đầy đủ).
- ❌ Không quản lý *hạ tầng deploy* của các microservice — chỉ quản lý *tài liệu* về chúng.
- ❌ Không thay thế Jira/Confluence cho quản lý công việc dự án — Portal là nơi *tài liệu chuẩn*, không phải nơi làm việc hằng ngày.
- ❌ Không tự động *sinh* tài liệu quy trình bằng AI trong bản này (BOT chỉ *đọc*, không *ban hành*).
- ❌ Không tính chi phí hạ tầng server / license / đào tạo — thuộc kế hoạch triển khai riêng.

---

## 13. Roadmap triển khai

| Phase | Nội dung | Kết quả dùng được |
|-------|----------|-------------------|
| **P1 — MVP** | DOC + KB + FBK + ADM | Có nguồn sự thật, tra cứu được, nhận feedback, phân quyền |
| **P2 — Vận hành & Đo** | ONB + RPT + review cycle + acknowledgement | Onboarding tự phục vụ, QA có báo cáo quản trị |
| **P3 — AI** | BOT (RAG) trên corpus đã ổn định | Hỏi–đáp có kiểm soát, trích nguồn |

---

## 14. Quyết định cần chốt (Open Decisions)

> Đây là mấy cái tao cần mày quyết trước khi đi sâu — không chốt thì làm tiếp là đoán mò.

| # | Quyết định | Lựa chọn | Tao nghiêng về |
|---|-----------|----------|----------------|
| OD-1 | **Build vs Buy** | (a) Docs-as-code: Git + MkDocs/Docusaurus + RAG · (b) Confluence/SharePoint + plugin · (c) Custom app | (a) — hợp triết lý Markdown/AI-readable & lean của mày, versioning bằng Git là tự nhiên |
| OD-2 | Tên & viết tắt platform | QGP / QMS Portal / khác | Chờ mày |
| OD-3 | Metric truy cập: aggregate hay per-user | Aggregate mặc định, per-user chỉ cho acknowledgement | Aggregate |
| OD-4 | Corpus BOT lấy từ DOC gốc hay cả KB | Chỉ DOC Effective, có thể thêm FAQ | Chỉ DOC + FAQ |
| OD-5 | Có bắt buộc `mandatory_ack` reset khi major version? | Có (theo §4.4) | Có |

---

*QMS Governance Portal — Spec v0.1 (Draft) · QA / Process Governance · Nội bộ FPT ISC*
