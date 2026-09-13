# ADR-0013 — Observability: OpenTelemetry + Grafana stack

- **Status:** Accepted
- **Date:** 2026-07-10
- **Deciders:** SA, DevOps

## Context
NFR-07 yêu cầu OpenTelemetry; cần logs/metrics/traces; open-source self-host.

## Decision
**OpenTelemetry** SDK → OTLP → **Prometheus** (metrics) + **Tempo** (traces) + **Loki** (logs) + **Grafana** (dashboards/alerts). Analytics events ở mức aggregate (ADR-0003).

## Consequences
**Tích cực:**
- Chuẩn nội bộ (NFR-07), vendor-neutral
- Self-host open-source
- RED/USE + tracing đầy đủ

**Tiêu cực / đánh đổi:**
- Vận hành thêm stack Grafana

## Alternatives considered
- **ELK** — Nặng hơn
- **APM thương mại** — Chi phí/lock-in

## Related
SDD §9, NFR-07, ADR-0003
