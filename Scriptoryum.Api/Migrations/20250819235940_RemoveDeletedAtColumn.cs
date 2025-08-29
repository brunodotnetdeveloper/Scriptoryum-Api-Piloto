using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Scriptoryum.Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDeletedAtColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "timeline_events");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "service_api_keys");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "risks_detected");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "insights");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "extracted_entities");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "company_users");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "company_a_i_provider_configs");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "chat_sessions");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "chat_messages");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "a_i_provider_configs");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "a_i_configurations");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "deleted_at",
                table: "timeline_events",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "deleted_at",
                table: "service_api_keys",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "deleted_at",
                table: "risks_detected",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "deleted_at",
                table: "notifications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "deleted_at",
                table: "insights",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "deleted_at",
                table: "extracted_entities",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "deleted_at",
                table: "documents",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "deleted_at",
                table: "company_users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "deleted_at",
                table: "company_a_i_provider_configs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "deleted_at",
                table: "companies",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "deleted_at",
                table: "chat_sessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "deleted_at",
                table: "chat_messages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "deleted_at",
                table: "a_i_provider_configs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "deleted_at",
                table: "a_i_configurations",
                type: "timestamp with time zone",
                nullable: true);
        }
    }
}
