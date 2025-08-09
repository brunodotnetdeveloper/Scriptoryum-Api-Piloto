using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Scriptoryum.Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLegacyAIConfigurationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "claude_api_key",
                table: "a_i_configurations");

            migrationBuilder.DropColumn(
                name: "claude_model",
                table: "a_i_configurations");

            migrationBuilder.DropColumn(
                name: "gemini_api_key",
                table: "a_i_configurations");

            migrationBuilder.DropColumn(
                name: "gemini_model",
                table: "a_i_configurations");

            migrationBuilder.DropColumn(
                name: "open_a_i_api_key",
                table: "a_i_configurations");

            migrationBuilder.DropColumn(
                name: "open_a_i_model",
                table: "a_i_configurations");

            migrationBuilder.AlterColumn<string>(
                name: "default_provider",
                table: "a_i_configurations",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "default_provider",
                table: "a_i_configurations",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "claude_api_key",
                table: "a_i_configurations",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "claude_model",
                table: "a_i_configurations",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "gemini_api_key",
                table: "a_i_configurations",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "gemini_model",
                table: "a_i_configurations",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "open_a_i_api_key",
                table: "a_i_configurations",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "open_a_i_model",
                table: "a_i_configurations",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }
    }
}
