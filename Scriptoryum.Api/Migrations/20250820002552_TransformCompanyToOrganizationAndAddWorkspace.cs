using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Scriptoryum.Api.Migrations
{
    /// <inheritdoc />
    public partial class TransformCompanyToOrganizationAndAddWorkspace : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "workspace_id",
                table: "service_api_keys",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "workspace_id",
                table: "documents",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "workspace_id",
                table: "chat_sessions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "organization_id",
                table: "a_i_configurations",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "workspace_id",
                table: "a_i_configurations",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "organizations",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    cnpj = table.Column<string>(type: "character varying(18)", maxLength: 18, nullable: true),
                    contact_email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    contact_phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    status = table.Column<string>(type: "text", nullable: false, defaultValue: "Active"),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_organizations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "organization_a_i_provider_configs",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    organization_id = table.Column<int>(type: "integer", nullable: false),
                    provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    api_key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    selected_model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    last_test_result = table.Column<bool>(type: "boolean", nullable: true),
                    last_test_message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    last_tested_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    monthly_token_limit = table.Column<int>(type: "integer", nullable: true),
                    tokens_used_this_month = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    token_counter_reset_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_organization_a_i_provider_configs", x => x.id);
                    table.ForeignKey(
                        name: "f_k_organization_a_i_provider_configs_organizations_organizatio~",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "organization_users",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    organization_id = table.Column<int>(type: "integer", nullable: false),
                    user_id = table.Column<string>(type: "text", nullable: true),
                    role = table.Column<string>(type: "text", nullable: false, defaultValue: "Member"),
                    status = table.Column<string>(type: "text", nullable: false, defaultValue: "Active"),
                    joined_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    removed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_organization_users", x => x.id);
                    table.ForeignKey(
                        name: "f_k_organization_users_asp_net_users_user_id",
                        column: x => x.user_id,
                        principalTable: "asp_net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "f_k_organization_users_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "workspaces",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    status = table.Column<string>(type: "text", nullable: false, defaultValue: "Active"),
                    organization_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_workspaces", x => x.id);
                    table.ForeignKey(
                        name: "f_k_workspaces_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "workspace_users",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    workspace_id = table.Column<int>(type: "integer", nullable: false),
                    user_id = table.Column<string>(type: "text", nullable: true),
                    role = table.Column<string>(type: "text", nullable: false, defaultValue: "Member"),
                    status = table.Column<string>(type: "text", nullable: false, defaultValue: "Active"),
                    joined_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    removed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_workspace_users", x => x.id);
                    table.ForeignKey(
                        name: "f_k_workspace_users_asp_net_users_user_id",
                        column: x => x.user_id,
                        principalTable: "asp_net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "f_k_workspace_users_workspaces_workspace_id",
                        column: x => x.workspace_id,
                        principalTable: "workspaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "i_x_service_api_keys_workspace_id",
                table: "service_api_keys",
                column: "workspace_id");

            migrationBuilder.CreateIndex(
                name: "i_x_documents_workspace_id",
                table: "documents",
                column: "workspace_id");

            migrationBuilder.CreateIndex(
                name: "i_x_chat_sessions_workspace_id",
                table: "chat_sessions",
                column: "workspace_id");

            migrationBuilder.CreateIndex(
                name: "i_x_a_i_configurations_organization_id",
                table: "a_i_configurations",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "i_x_a_i_configurations_workspace_id",
                table: "a_i_configurations",
                column: "workspace_id");

            migrationBuilder.CreateIndex(
                name: "i_x_organization_a_i_provider_configs_organization_id",
                table: "organization_a_i_provider_configs",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "i_x_organization_users_user_id",
                table: "organization_users",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_organization_users_organization_id_user_id",
                table: "organization_users",
                columns: new[] { "organization_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_workspace_users_user_id",
                table: "workspace_users",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_workspace_users_workspace_id_user_id",
                table: "workspace_users",
                columns: new[] { "workspace_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_workspaces_organization_id",
                table: "workspaces",
                column: "organization_id");

            migrationBuilder.AddForeignKey(
                name: "f_k_a_i_configurations_organizations_organization_id",
                table: "a_i_configurations",
                column: "organization_id",
                principalTable: "organizations",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "f_k_a_i_configurations_workspaces_workspace_id",
                table: "a_i_configurations",
                column: "workspace_id",
                principalTable: "workspaces",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "f_k_chat_sessions_workspaces_workspace_id",
                table: "chat_sessions",
                column: "workspace_id",
                principalTable: "workspaces",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "f_k_documents_workspaces_workspace_id",
                table: "documents",
                column: "workspace_id",
                principalTable: "workspaces",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "f_k_service_api_keys_workspaces_workspace_id",
                table: "service_api_keys",
                column: "workspace_id",
                principalTable: "workspaces",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "f_k_a_i_configurations_organizations_organization_id",
                table: "a_i_configurations");

            migrationBuilder.DropForeignKey(
                name: "f_k_a_i_configurations_workspaces_workspace_id",
                table: "a_i_configurations");

            migrationBuilder.DropForeignKey(
                name: "f_k_chat_sessions_workspaces_workspace_id",
                table: "chat_sessions");

            migrationBuilder.DropForeignKey(
                name: "f_k_documents_workspaces_workspace_id",
                table: "documents");

            migrationBuilder.DropForeignKey(
                name: "f_k_service_api_keys_workspaces_workspace_id",
                table: "service_api_keys");

            migrationBuilder.DropTable(
                name: "organization_a_i_provider_configs");

            migrationBuilder.DropTable(
                name: "organization_users");

            migrationBuilder.DropTable(
                name: "workspace_users");

            migrationBuilder.DropTable(
                name: "workspaces");

            migrationBuilder.DropTable(
                name: "organizations");

            migrationBuilder.DropIndex(
                name: "i_x_service_api_keys_workspace_id",
                table: "service_api_keys");

            migrationBuilder.DropIndex(
                name: "i_x_documents_workspace_id",
                table: "documents");

            migrationBuilder.DropIndex(
                name: "i_x_chat_sessions_workspace_id",
                table: "chat_sessions");

            migrationBuilder.DropIndex(
                name: "i_x_a_i_configurations_organization_id",
                table: "a_i_configurations");

            migrationBuilder.DropIndex(
                name: "i_x_a_i_configurations_workspace_id",
                table: "a_i_configurations");

            migrationBuilder.DropColumn(
                name: "workspace_id",
                table: "service_api_keys");

            migrationBuilder.DropColumn(
                name: "workspace_id",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "workspace_id",
                table: "chat_sessions");

            migrationBuilder.DropColumn(
                name: "organization_id",
                table: "a_i_configurations");

            migrationBuilder.DropColumn(
                name: "workspace_id",
                table: "a_i_configurations");
        }
    }
}
