using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qgp.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVersionContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "content_markdown",
                schema: "qgp",
                table: "document_versions",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "content_markdown",
                schema: "qgp",
                table: "document_versions");
        }
    }
}
