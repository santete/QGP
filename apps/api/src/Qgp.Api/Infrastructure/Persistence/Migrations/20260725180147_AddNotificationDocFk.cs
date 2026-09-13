using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qgp.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationDocFk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_notifications_document_id",
                schema: "qgp",
                table: "notifications",
                column: "document_id");

            migrationBuilder.AddForeignKey(
                name: "fk_notifications_documents_document_id",
                schema: "qgp",
                table: "notifications",
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
                name: "fk_notifications_documents_document_id",
                schema: "qgp",
                table: "notifications");

            migrationBuilder.DropIndex(
                name: "ix_notifications_document_id",
                schema: "qgp",
                table: "notifications");
        }
    }
}
