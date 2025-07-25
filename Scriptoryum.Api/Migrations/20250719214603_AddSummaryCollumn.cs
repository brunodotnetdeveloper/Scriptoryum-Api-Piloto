using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Scriptoryum.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSummaryCollumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "summary",
                table: "documents",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "summary",
                table: "documents");
        }
    }
}
