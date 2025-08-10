using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Scriptoryum.Api.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDataModel4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "service_api_keys",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    service_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    api_key_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    key_prefix = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    key_suffix = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    status = table.Column<string>(type: "text", nullable: false, defaultValue: "Active"),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    usage_count = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    monthly_usage_limit = table.Column<long>(type: "bigint", nullable: true),
                    current_month_usage = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    current_month_year = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    permissions = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    allowed_i_ps = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_by_user_id = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_service_api_keys", x => x.id);
                    table.ForeignKey(
                        name: "f_k_service_api_keys_asp_net_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "asp_net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "i_x_service_api_keys_created_by_user_id",
                table: "service_api_keys",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_service_api_keys_api_key_hash",
                table: "service_api_keys",
                column: "api_key_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_service_api_keys_expires_at",
                table: "service_api_keys",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "IX_service_api_keys_status",
                table: "service_api_keys",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "service_api_keys");
        }
    }
}
