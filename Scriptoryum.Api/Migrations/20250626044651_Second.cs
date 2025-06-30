using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Scriptoryum.Api.Migrations
{
    /// <inheritdoc />
    public partial class Second : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "analysis_summary",
                table: "documents");

            migrationBuilder.AddColumn<string>(
                name: "text_extracted",
                table: "documents",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "text_extracted",
                table: "documents");

            migrationBuilder.AddColumn<string>(
                name: "analysis_summary",
                table: "documents",
                type: "jsonb",
                nullable: true);
        }
    }
}
