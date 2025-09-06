using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Scriptoryum.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddEntitiesDynamicFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "document_type_id",
                table: "documents",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "document_type_templates",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    template_data = table.Column<string>(type: "jsonb", nullable: false),
                    is_public = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    organization_id = table.Column<int>(type: "integer", nullable: true),
                    usage_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_by_user_id = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_document_type_templates", x => x.id);
                    table.ForeignKey(
                        name: "f_k_document_type_templates_asp_net_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "asp_net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "f_k_document_type_templates_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "document_types",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    organization_id = table.Column<int>(type: "integer", nullable: false),
                    is_system_default = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Active"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_document_types", x => x.id);
                    table.ForeignKey(
                        name: "f_k_document_types_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "document_type_fields",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    document_type_id = table.Column<int>(type: "integer", nullable: false),
                    field_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    field_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    extraction_prompt = table.Column<string>(type: "text", nullable: true),
                    is_required = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    validation_regex = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    default_value = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    field_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Active"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_document_type_fields", x => x.id);
                    table.ForeignKey(
                        name: "f_k_document_type_fields_document_types_document_type_id",
                        column: x => x.document_type_id,
                        principalTable: "document_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "document_field_values",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    document_id = table.Column<int>(type: "integer", nullable: false),
                    document_type_field_id = table.Column<int>(type: "integer", nullable: false),
                    extracted_value = table.Column<string>(type: "text", nullable: true),
                    confidence_score = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: true, defaultValue: 0.0m),
                    context_excerpt = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    start_position = table.Column<int>(type: "integer", nullable: true),
                    end_position = table.Column<int>(type: "integer", nullable: true),
                    validation_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    validated_by_user_id = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    validated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    corrected_value = table.Column<string>(type: "text", nullable: true),
                    extraction_metadata = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_document_field_values", x => x.id);
                    table.ForeignKey(
                        name: "f_k_document_field_values_asp_net_users_validated_by_user_id",
                        column: x => x.validated_by_user_id,
                        principalTable: "asp_net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "f_k_document_field_values_document_type_fields_document_type_fiel~",
                        column: x => x.document_type_field_id,
                        principalTable: "document_type_fields",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "f_k_document_field_values_documents_document_id",
                        column: x => x.document_id,
                        principalTable: "documents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "document_field_value_histories",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    document_field_value_id = table.Column<int>(type: "integer", nullable: false),
                    previous_value = table.Column<string>(type: "text", nullable: true),
                    new_value = table.Column<string>(type: "text", nullable: true),
                    change_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    changed_by_user_id = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    change_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    confidence_score_before = table.Column<decimal>(type: "numeric(5,4)", nullable: true),
                    confidence_score_after = table.Column<decimal>(type: "numeric(5,4)", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_document_field_value_histories", x => x.id);
                    table.ForeignKey(
                        name: "f_k_document_field_value_histories_asp_net_users_changed_by_use~",
                        column: x => x.changed_by_user_id,
                        principalTable: "asp_net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "f_k_document_field_value_histories_document_field_values_docume~",
                        column: x => x.document_field_value_id,
                        principalTable: "document_field_values",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "i_x_documents_document_type_id",
                table: "documents",
                column: "document_type_id");

            migrationBuilder.CreateIndex(
                name: "i_x_document_field_value_histories_changed_by_user_id",
                table: "document_field_value_histories",
                column: "changed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "i_x_document_field_value_histories_document_field_value_id",
                table: "document_field_value_histories",
                column: "document_field_value_id");

            migrationBuilder.CreateIndex(
                name: "IX_document_field_value_histories_updated_at",
                table: "document_field_value_histories",
                column: "updated_at");

            migrationBuilder.CreateIndex(
                name: "i_x_document_field_values_document_type_field_id",
                table: "document_field_values",
                column: "document_type_field_id");

            migrationBuilder.CreateIndex(
                name: "i_x_document_field_values_validated_by_user_id",
                table: "document_field_values",
                column: "validated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_document_field_values_document_id_document_type_field_id",
                table: "document_field_values",
                columns: new[] { "document_id", "document_type_field_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_document_field_values_validated_at",
                table: "document_field_values",
                column: "validated_at");

            migrationBuilder.CreateIndex(
                name: "IX_document_field_values_validation_status",
                table: "document_field_values",
                column: "validation_status");

            migrationBuilder.CreateIndex(
                name: "IX_document_type_fields_document_type_id_field_name",
                table: "document_type_fields",
                columns: new[] { "document_type_id", "field_name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_document_type_fields_field_order",
                table: "document_type_fields",
                column: "field_order");

            migrationBuilder.CreateIndex(
                name: "i_x_document_type_templates_created_by_user_id",
                table: "document_type_templates",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "i_x_document_type_templates_organization_id",
                table: "document_type_templates",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_document_type_templates_category",
                table: "document_type_templates",
                column: "category");

            migrationBuilder.CreateIndex(
                name: "IX_document_type_templates_is_public",
                table: "document_type_templates",
                column: "is_public");

            migrationBuilder.CreateIndex(
                name: "IX_document_type_templates_usage_count",
                table: "document_type_templates",
                column: "usage_count");

            migrationBuilder.CreateIndex(
                name: "IX_document_types_organization_id_name",
                table: "document_types",
                columns: new[] { "organization_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_document_types_status",
                table: "document_types",
                column: "status");

            migrationBuilder.AddForeignKey(
                name: "f_k_documents_document_types_document_type_id",
                table: "documents",
                column: "document_type_id",
                principalTable: "document_types",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "f_k_documents_document_types_document_type_id",
                table: "documents");

            migrationBuilder.DropTable(
                name: "document_field_value_histories");

            migrationBuilder.DropTable(
                name: "document_type_templates");

            migrationBuilder.DropTable(
                name: "document_field_values");

            migrationBuilder.DropTable(
                name: "document_type_fields");

            migrationBuilder.DropTable(
                name: "document_types");

            migrationBuilder.DropIndex(
                name: "i_x_documents_document_type_id",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "document_type_id",
                table: "documents");
        }
    }
}
