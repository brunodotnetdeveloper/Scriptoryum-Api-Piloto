using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Scriptoryum.Api.Migrations
{
    /// <inheritdoc />
    public partial class UpdateServiceApiKeyWithOrganizationAndWorkspace : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "f_k_a_i_configurations_companies_company_id",
                table: "a_i_configurations");

            migrationBuilder.DropForeignKey(
                name: "f_k_service_api_keys_workspaces_workspace_id",
                table: "service_api_keys");

            migrationBuilder.DropTable(
                name: "company_a_i_provider_configs");

            migrationBuilder.DropTable(
                name: "company_users");

            migrationBuilder.DropTable(
                name: "companies");

            migrationBuilder.DropIndex(
                name: "i_x_a_i_configurations_company_id",
                table: "a_i_configurations");

            migrationBuilder.DropColumn(
                name: "company_id",
                table: "a_i_configurations");

            migrationBuilder.AddColumn<int>(
                name: "organization_id",
                table: "service_api_keys",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "i_x_service_api_keys_organization_id",
                table: "service_api_keys",
                column: "organization_id");

            migrationBuilder.AddForeignKey(
                name: "f_k_service_api_keys_organizations_organization_id",
                table: "service_api_keys",
                column: "organization_id",
                principalTable: "organizations",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "f_k_service_api_keys_workspaces_workspace_id",
                table: "service_api_keys",
                column: "workspace_id",
                principalTable: "workspaces",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "f_k_service_api_keys_organizations_organization_id",
                table: "service_api_keys");

            migrationBuilder.DropForeignKey(
                name: "f_k_service_api_keys_workspaces_workspace_id",
                table: "service_api_keys");

            migrationBuilder.DropIndex(
                name: "i_x_service_api_keys_organization_id",
                table: "service_api_keys");

            migrationBuilder.DropColumn(
                name: "organization_id",
                table: "service_api_keys");

            migrationBuilder.AddColumn<int>(
                name: "company_id",
                table: "a_i_configurations",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "companies",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    address = table.Column<string>(type: "text", nullable: true),
                    cnpj = table.Column<string>(type: "text", nullable: true),
                    contact_email = table.Column<string>(type: "text", nullable: true),
                    contact_phone = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    name = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "text", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_companies", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "company_a_i_provider_configs",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    company_id = table.Column<int>(type: "integer", nullable: false),
                    api_key = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    last_test_message = table.Column<string>(type: "text", nullable: true),
                    last_test_result = table.Column<bool>(type: "boolean", nullable: true),
                    last_tested_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    monthly_token_limit = table.Column<int>(type: "integer", nullable: true),
                    provider = table.Column<string>(type: "text", nullable: true),
                    selected_model = table.Column<string>(type: "text", nullable: true),
                    token_counter_reset_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    tokens_used_this_month = table.Column<int>(type: "integer", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_company_a_i_provider_configs", x => x.id);
                    table.ForeignKey(
                        name: "f_k_company_a_i_provider_configs_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "company_users",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    company_id = table.Column<int>(type: "integer", nullable: false),
                    user_id = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    joined_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    removed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    role = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_company_users", x => x.id);
                    table.ForeignKey(
                        name: "f_k_company_users_asp_net_users_user_id",
                        column: x => x.user_id,
                        principalTable: "asp_net_users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "f_k_company_users_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "i_x_a_i_configurations_company_id",
                table: "a_i_configurations",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "i_x_company_a_i_provider_configs_company_id",
                table: "company_a_i_provider_configs",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "i_x_company_users_company_id",
                table: "company_users",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "i_x_company_users_user_id",
                table: "company_users",
                column: "user_id");

            migrationBuilder.AddForeignKey(
                name: "f_k_a_i_configurations_companies_company_id",
                table: "a_i_configurations",
                column: "company_id",
                principalTable: "companies",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "f_k_service_api_keys_workspaces_workspace_id",
                table: "service_api_keys",
                column: "workspace_id",
                principalTable: "workspaces",
                principalColumn: "id");
        }
    }
}
