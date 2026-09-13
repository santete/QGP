using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qgp.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOnboarding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "learning_paths",
                schema: "qgp",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_learning_paths", x => x.id);
                    table.ForeignKey(
                        name: "fk_learning_paths_roles_role_id",
                        column: x => x.role_id,
                        principalSchema: "qgp",
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "path_items",
                schema: "qgp",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    learning_path_id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_id = table.Column<Guid>(type: "uuid", nullable: false),
                    seq = table.Column<int>(type: "integer", nullable: false),
                    mandatory = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_path_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_path_items_documents_document_id",
                        column: x => x.document_id,
                        principalSchema: "qgp",
                        principalTable: "documents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_path_items_learning_paths_learning_path_id",
                        column: x => x.learning_path_id,
                        principalSchema: "qgp",
                        principalTable: "learning_paths",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "uq_learning_path_role",
                schema: "qgp",
                table: "learning_paths",
                column: "role_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_path_items_document_id",
                schema: "qgp",
                table: "path_items",
                column: "document_id");

            migrationBuilder.CreateIndex(
                name: "path_items_path_seq_idx",
                schema: "qgp",
                table: "path_items",
                columns: new[] { "learning_path_id", "seq" });

            migrationBuilder.CreateIndex(
                name: "uq_path_item_doc",
                schema: "qgp",
                table: "path_items",
                columns: new[] { "learning_path_id", "document_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "path_items",
                schema: "qgp");

            migrationBuilder.DropTable(
                name: "learning_paths",
                schema: "qgp");
        }
    }
}
