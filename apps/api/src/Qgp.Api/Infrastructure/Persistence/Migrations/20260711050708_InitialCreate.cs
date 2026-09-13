using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Qgp.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "qgp");

            migrationBuilder.CreateTable(
                name: "audit_logs",
                schema: "qgp",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    document_id = table.Column<Guid>(type: "uuid", nullable: true),
                    at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                schema: "qgp",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tags",
                schema: "qgp",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    slug = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tags", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                schema: "qgp",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    sso_subject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_roles",
                schema: "qgp",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_roles", x => new { x.user_id, x.role_id });
                    table.ForeignKey(
                        name: "fk_user_roles_roles_role_id",
                        column: x => x.role_id,
                        principalSchema: "qgp",
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_user_roles_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "qgp",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "acknowledgements",
                schema: "qgp",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    acked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_acknowledgements", x => x.id);
                    table.ForeignKey(
                        name: "fk_acknowledgements_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "qgp",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "doc_audience_roles",
                schema: "qgp",
                columns: table => new
                {
                    document_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_doc_audience_roles", x => new { x.document_id, x.role_id });
                    table.ForeignKey(
                        name: "fk_doc_audience_roles_roles_role_id",
                        column: x => x.role_id,
                        principalSchema: "qgp",
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "doc_relations",
                schema: "qgp",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    source_document_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_document_id = table.Column<Guid>(type: "uuid", nullable: false),
                    relation_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_doc_relations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "doc_tags",
                schema: "qgp",
                columns: table => new
                {
                    document_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tag_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_doc_tags", x => new { x.document_id, x.tag_id });
                    table.ForeignKey(
                        name: "fk_doc_tags_tags_tag_id",
                        column: x => x.tag_id,
                        principalSchema: "qgp",
                        principalTable: "tags",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "document_versions",
                schema: "qgp",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    document_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    issue_date = table.Column<DateOnly>(type: "date", nullable: true),
                    effective_date = table.Column<DateOnly>(type: "date", nullable: true),
                    change_summary = table.Column<string>(type: "text", nullable: true),
                    content_git_ref = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_document_versions", x => x.id);
                    table.CheckConstraint("ck_effective_ge_issue", "effective_date IS NULL OR issue_date IS NULL OR effective_date >= issue_date");
                });

            migrationBuilder.CreateTable(
                name: "documents",
                schema: "qgp",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    doc_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    classification = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    current_effective_version_id = table.Column<Guid>(type: "uuid", nullable: true),
                    mandatory_ack = table.Column<bool>(type: "boolean", nullable: false),
                    next_review_date = table.Column<DateOnly>(type: "date", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_documents", x => x.id);
                    table.ForeignKey(
                        name: "fk_documents_document_versions_current_effective_version_id",
                        column: x => x.current_effective_version_id,
                        principalSchema: "qgp",
                        principalTable: "document_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "feedback",
                schema: "qgp",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    body = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_feedback", x => x.id);
                    table.ForeignKey(
                        name: "fk_feedback_document_versions_version_id",
                        column: x => x.version_id,
                        principalSchema: "qgp",
                        principalTable: "document_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_feedback_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "qgp",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "qgp",
                table: "roles",
                columns: new[] { "id", "code", "created_at", "name", "updated_at" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111101"), "READER", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Reader", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("11111111-1111-1111-1111-111111111102"), "CONTRIBUTOR", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Contributor", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("11111111-1111-1111-1111-111111111103"), "AUTHOR", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Author", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("11111111-1111-1111-1111-111111111104"), "APPROVER", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Approver", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("11111111-1111-1111-1111-111111111105"), "QA_LEAD", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "QA Lead", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("11111111-1111-1111-1111-111111111106"), "ADMIN", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Admin", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.CreateIndex(
                name: "ix_acknowledgements_version_id",
                schema: "qgp",
                table: "acknowledgements",
                column: "version_id");

            migrationBuilder.CreateIndex(
                name: "uq_ack_user_version",
                schema: "qgp",
                table: "acknowledgements",
                columns: new[] { "user_id", "version_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "audit_log_at_idx",
                schema: "qgp",
                table: "audit_logs",
                column: "at");

            migrationBuilder.CreateIndex(
                name: "audit_log_document_id_idx",
                schema: "qgp",
                table: "audit_logs",
                column: "document_id");

            migrationBuilder.CreateIndex(
                name: "doc_audience_roles_role_id_idx",
                schema: "qgp",
                table: "doc_audience_roles",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ix_doc_relations_target_document_id",
                schema: "qgp",
                table: "doc_relations",
                column: "target_document_id");

            migrationBuilder.CreateIndex(
                name: "uq_doc_relation",
                schema: "qgp",
                table: "doc_relations",
                columns: new[] { "source_document_id", "target_document_id", "relation_type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_doc_tags_tag_id",
                schema: "qgp",
                table: "doc_tags",
                column: "tag_id");

            migrationBuilder.CreateIndex(
                name: "document_versions_status_effective_date_idx",
                schema: "qgp",
                table: "document_versions",
                columns: new[] { "status", "effective_date" });

            migrationBuilder.CreateIndex(
                name: "uq_one_effective_per_doc",
                schema: "qgp",
                table: "document_versions",
                column: "document_id",
                unique: true,
                filter: "status = 'Effective'");

            migrationBuilder.CreateIndex(
                name: "ix_documents_current_effective_version_id",
                schema: "qgp",
                table: "documents",
                column: "current_effective_version_id");

            migrationBuilder.CreateIndex(
                name: "uq_doc_id",
                schema: "qgp",
                table: "documents",
                column: "doc_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "feedback_status_created_at_idx",
                schema: "qgp",
                table: "feedback",
                columns: new[] { "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_feedback_user_id",
                schema: "qgp",
                table: "feedback",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_feedback_version_id",
                schema: "qgp",
                table: "feedback",
                column: "version_id");

            migrationBuilder.CreateIndex(
                name: "uq_roles_code",
                schema: "qgp",
                table: "roles",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_tags_slug",
                schema: "qgp",
                table: "tags",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_roles_role_id",
                schema: "qgp",
                table: "user_roles",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "uq_users_sso_subject",
                schema: "qgp",
                table: "users",
                column: "sso_subject",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_acknowledgements_document_versions_version_id",
                schema: "qgp",
                table: "acknowledgements",
                column: "version_id",
                principalSchema: "qgp",
                principalTable: "document_versions",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_doc_audience_roles_documents_document_id",
                schema: "qgp",
                table: "doc_audience_roles",
                column: "document_id",
                principalSchema: "qgp",
                principalTable: "documents",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_doc_relations_documents_source_document_id",
                schema: "qgp",
                table: "doc_relations",
                column: "source_document_id",
                principalSchema: "qgp",
                principalTable: "documents",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_doc_relations_documents_target_document_id",
                schema: "qgp",
                table: "doc_relations",
                column: "target_document_id",
                principalSchema: "qgp",
                principalTable: "documents",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_doc_tags_documents_document_id",
                schema: "qgp",
                table: "doc_tags",
                column: "document_id",
                principalSchema: "qgp",
                principalTable: "documents",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_document_versions_documents_document_id",
                schema: "qgp",
                table: "document_versions",
                column: "document_id",
                principalSchema: "qgp",
                principalTable: "documents",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_documents_document_versions_current_effective_version_id",
                schema: "qgp",
                table: "documents");

            migrationBuilder.DropTable(
                name: "acknowledgements",
                schema: "qgp");

            migrationBuilder.DropTable(
                name: "audit_logs",
                schema: "qgp");

            migrationBuilder.DropTable(
                name: "doc_audience_roles",
                schema: "qgp");

            migrationBuilder.DropTable(
                name: "doc_relations",
                schema: "qgp");

            migrationBuilder.DropTable(
                name: "doc_tags",
                schema: "qgp");

            migrationBuilder.DropTable(
                name: "feedback",
                schema: "qgp");

            migrationBuilder.DropTable(
                name: "user_roles",
                schema: "qgp");

            migrationBuilder.DropTable(
                name: "tags",
                schema: "qgp");

            migrationBuilder.DropTable(
                name: "roles",
                schema: "qgp");

            migrationBuilder.DropTable(
                name: "users",
                schema: "qgp");

            migrationBuilder.DropTable(
                name: "document_versions",
                schema: "qgp");

            migrationBuilder.DropTable(
                name: "documents",
                schema: "qgp");
        }
    }
}
