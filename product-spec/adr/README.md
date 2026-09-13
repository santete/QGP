# Architecture Decision Records — QGP

> ADR theo docs-as-code (mỗi quyết định 1 file, immutable; đổi ý → ADR mới 'Supersedes'). Map từ PRD Open Decisions (OD-1..5) + quyết định stack SDD.

| ADR | Quyết định | Status | Nguồn |
|---|---|---|---|
| [ADR-0001](0001-docs-as-code-architecture.md) | Docs-as-code architecture | Accepted | OD-1 |
| [ADR-0002](0002-platform-name-qgp.md) | Tên nền tảng: Quality Governance Portal (QGP) | Accepted | OD-2 |
| [ADR-0003](0003-aggregate-access-metrics.md) | Metric truy cập aggregate mặc định | Accepted | OD-3 |
| [ADR-0004](0004-bot-corpus-effective-only.md) | BOT chỉ dùng corpus DOC Effective + FAQ | Accepted | OD-4 |
| [ADR-0005](0005-reset-ack-on-major.md) | Reset acknowledgement khi major version | Accepted | OD-5 |
| [ADR-0006](0006-backend-dotnet.md) | Backend: .NET 8 ASP.NET Core | Accepted | SDD stack |
| [ADR-0007](0007-rag-python-service.md) | RAG là service Python riêng (hybrid .NET + Python) | Accepted | SDD stack |
| [ADR-0008](0008-postgres-pgvector.md) | Datastore: PostgreSQL 16 + pgvector | Accepted | SDD stack |
| [ADR-0009](0009-meilisearch-search.md) | Full-text search: Meilisearch | Accepted | SDD stack |
| [ADR-0010](0010-frontend-react.md) | Frontend: React 18 + Vite + TypeScript | Accepted | SDD stack |
| [ADR-0011](0011-gitlab-ci-docker.md) | CI/CD: GitLab CI + Docker trên Ubuntu | Accepted | SDD stack |
| [ADR-0012](0012-vault-secrets.md) | Secrets: HashiCorp Vault | Accepted | SDD stack |
| [ADR-0013](0013-otel-observability.md) | Observability: OpenTelemetry + Grafana stack | Accepted | SDD stack |

## Format
MADR-lite: Context → Decision → Consequences (±) → Alternatives → Related. File đặt tên `NNNN-kebab-title.md`, không sửa nội dung đã Accepted; thay đổi = ADR mới.

## Related docs
- PRD: `../PRD_QGP_v1.0.docx`
- SDD: `../SDD_QGP_v0.3.md`
