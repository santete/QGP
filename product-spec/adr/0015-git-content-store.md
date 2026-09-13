# ADR-0015 — Git content store (LibGit2Sharp, commit-on-publish)

- **Status:** Accepted
- **Date:** 2026-07-25
- **Deciders:** PO (PhucDN7), SA
- **Implements:** ADR-0001 (docs-as-code)

## Context
ADR-0001 quy định nội dung normative của tài liệu lưu trong **Git** (`content_git_ref`, bất biến theo commit — BR-03), Postgres giữ metadata/lifecycle. Ở E1.1 nội dung được **tạm** lưu cột DB `content_markdown` để chạy vertical slice; `content_git_ref` bỏ trống — nợ kỹ thuật lệch ADR-0001. Hạ tầng GitLab (ADR-0011) **chưa dựng** (Q2 defer, chạy docker-compose local trước). Cần hiện thực Git store mà không phụ thuộc GitLab.

## Decision
Dùng **bare/working Git repo cục bộ** trên data volume của API, thao tác qua **LibGit2Sharp** (không cần Git CLI / GitLab):
- **Commit-on-publish**: khi `publish` (ranh giới bất biến BR-03), API ghi `{doc_id}/{version}.md` + commit → lưu **commit SHA** vào `content_git_ref`. Draft (`PATCH content`) vẫn ở DB `content_markdown` (mutable), chưa commit.
- Repo path qua `QGP_GIT_REPO_PATH` > config `Git:RepoPath` > mặc định `content-repo`.
- Commit serialize bằng lock (index libgit2 không thread-safe; 1 instance API).
- Đường nâng cấp: thêm `git push` lên GitLab khi hạ tầng sẵn — **không phải migrate dữ liệu**.

## Consequences
**Tích cực:**
- Đúng ADR-0001: nội dung bất biến trong Git; DR source-of-truth (SDD §) — mất Postgres vẫn khôi phục nội dung.
- Không cần GitLab để chạy MVP; hợp việc defer hạ tầng (Q2).
- Diff (E1.5) tái dùng: đọc 2 blob theo SHA → `UnifiedDiff` (đã có).

**Tiêu cển / đánh đổi:**
- Thêm dependency **LibGit2Sharp** (native lib) + cần **volume bền + backup** repo.
- Commit serialize → throughput publish giới hạn (chấp nhận được: publish hiếm).
- `content_markdown` DB thành cache/working-copy song song với Git (nguồn sự thật = Git sau publish).

## Alternatives considered
- **B. GitLab remote (push khi publish)** — Chuẩn FPT, MR-native, offsite; nhưng cần GitLab + token, publish phụ thuộc mạng, khó dev local → hoãn tới khi có hạ tầng.
- **C. Giữ DB (defer Git store)** — Đơn giản nhất nhưng lệch ADR-0001 + mất DR story Git; loại.

## Related
ADR-0001, ADR-0011, SDD §2.3 / DR §, BR-03, E1.6.
