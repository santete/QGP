using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qgp.Api.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Chuyển qgp.audit_logs sang declarative partitioning theo tháng (RANGE(at), SDD §2.4).
    /// Postgres không convert bảng thường → partitioned tại chỗ: tạo bảng mới, copy dữ liệu, swap.
    /// PK đổi (id) → (id, at) vì partition key phải nằm trong PK. DEFAULT partition đảm bảo insert
    /// không bao giờ vỡ; Quartz AuditPartitionMaintenanceJob tạo partition tháng kế.
    /// </summary>
    public partial class PartitionAuditLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Bảng partitioned mới (tên tạm) — PK gồm cột partition (id, at).
            migrationBuilder.Sql(@"
                CREATE TABLE qgp.audit_logs_new (
                    id uuid NOT NULL DEFAULT gen_random_uuid(),
                    actor_id uuid NULL,
                    action varchar(100) NOT NULL,
                    document_id uuid NULL,
                    at timestamptz NOT NULL,
                    created_at timestamptz NOT NULL DEFAULT now(),
                    CONSTRAINT pk_audit_logs_new PRIMARY KEY (id, at)
                ) PARTITION BY RANGE (at);");

            // 2. Partition tháng hiện tại + kế + DEFAULT (bắt mọi khoảng chưa có partition → không vỡ insert).
            migrationBuilder.Sql(
                "CREATE TABLE qgp.audit_logs_2026_07 PARTITION OF qgp.audit_logs_new FOR VALUES FROM ('2026-07-01') TO ('2026-08-01');");
            migrationBuilder.Sql(
                "CREATE TABLE qgp.audit_logs_2026_08 PARTITION OF qgp.audit_logs_new FOR VALUES FROM ('2026-08-01') TO ('2026-09-01');");
            migrationBuilder.Sql(
                "CREATE TABLE qgp.audit_logs_default PARTITION OF qgp.audit_logs_new DEFAULT;");

            // 3. Copy dữ liệu cũ.
            migrationBuilder.Sql(@"
                INSERT INTO qgp.audit_logs_new (id, actor_id, action, document_id, at, created_at)
                SELECT id, actor_id, action, document_id, at, created_at FROM qgp.audit_logs;");

            // 4. Drop bảng cũ, đổi tên bảng mới + PK về tên chuẩn (khớp EF snapshot).
            migrationBuilder.Sql("DROP TABLE qgp.audit_logs;");
            migrationBuilder.Sql("ALTER TABLE qgp.audit_logs_new RENAME TO audit_logs;");
            migrationBuilder.Sql("ALTER TABLE qgp.audit_logs RENAME CONSTRAINT pk_audit_logs_new TO pk_audit_logs;");

            // 5. Index (tên đã giải phóng sau khi drop bảng cũ).
            migrationBuilder.Sql("CREATE INDEX audit_log_at_idx ON qgp.audit_logs (at);");
            migrationBuilder.Sql("CREATE INDEX audit_log_document_id_idx ON qgp.audit_logs (document_id);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Quay lại bảng thường (PK id).
            migrationBuilder.Sql(@"
                CREATE TABLE qgp.audit_logs_normal (
                    id uuid NOT NULL DEFAULT gen_random_uuid(),
                    actor_id uuid NULL,
                    action varchar(100) NOT NULL,
                    document_id uuid NULL,
                    at timestamptz NOT NULL,
                    created_at timestamptz NOT NULL DEFAULT now(),
                    CONSTRAINT pk_audit_logs_normal PRIMARY KEY (id)
                );");
            migrationBuilder.Sql(@"
                INSERT INTO qgp.audit_logs_normal (id, actor_id, action, document_id, at, created_at)
                SELECT id, actor_id, action, document_id, at, created_at FROM qgp.audit_logs;");
            migrationBuilder.Sql("DROP TABLE qgp.audit_logs;"); // drop partitioned + mọi partition con
            migrationBuilder.Sql("ALTER TABLE qgp.audit_logs_normal RENAME TO audit_logs;");
            migrationBuilder.Sql("ALTER TABLE qgp.audit_logs RENAME CONSTRAINT pk_audit_logs_normal TO pk_audit_logs;");
            migrationBuilder.Sql("CREATE INDEX audit_log_at_idx ON qgp.audit_logs (at);");
            migrationBuilder.Sql("CREATE INDEX audit_log_document_id_idx ON qgp.audit_logs (document_id);");
        }
    }
}
