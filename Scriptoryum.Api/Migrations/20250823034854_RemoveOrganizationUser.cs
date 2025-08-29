using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Scriptoryum.Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveOrganizationUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "organization_users");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "joined_at",
                table: "asp_net_users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "organization_id",
                table: "asp_net_users",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "removed_at",
                table: "asp_net_users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "role",
                table: "asp_net_users",
                type: "text",
                nullable: false,
                defaultValue: "Member");

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "asp_net_users",
                type: "text",
                nullable: false,
                defaultValue: "Active");

            migrationBuilder.CreateIndex(
                name: "i_x_asp_net_users_organization_id",
                table: "asp_net_users",
                column: "organization_id");

            migrationBuilder.AddForeignKey(
                name: "f_k_asp_net_users_organizations_organization_id",
                table: "asp_net_users",
                column: "organization_id",
                principalTable: "organizations",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "f_k_asp_net_users_organizations_organization_id",
                table: "asp_net_users");

            migrationBuilder.DropIndex(
                name: "i_x_asp_net_users_organization_id",
                table: "asp_net_users");

            migrationBuilder.DropColumn(
                name: "joined_at",
                table: "asp_net_users");

            migrationBuilder.DropColumn(
                name: "organization_id",
                table: "asp_net_users");

            migrationBuilder.DropColumn(
                name: "removed_at",
                table: "asp_net_users");

            migrationBuilder.DropColumn(
                name: "role",
                table: "asp_net_users");

            migrationBuilder.DropColumn(
                name: "status",
                table: "asp_net_users");

            migrationBuilder.CreateTable(
                name: "organization_users",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    organization_id = table.Column<int>(type: "integer", nullable: false),
                    user_id = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    joined_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    removed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    role = table.Column<string>(type: "text", nullable: false, defaultValue: "Member"),
                    status = table.Column<string>(type: "text", nullable: false, defaultValue: "Active"),
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

            migrationBuilder.CreateIndex(
                name: "i_x_organization_users_user_id",
                table: "organization_users",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_organization_users_organization_id_user_id",
                table: "organization_users",
                columns: new[] { "organization_id", "user_id" },
                unique: true);
        }
    }
}
