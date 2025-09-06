using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Scriptoryum.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationalIntegrityConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "f_k_document_type_templates_organizations_organization_id",
                table: "document_type_templates");

            migrationBuilder.DropForeignKey(
                name: "f_k_documents_document_types_document_type_id",
                table: "documents");

            migrationBuilder.DropForeignKey(
                name: "f_k_documents_workspaces_workspace_id",
                table: "documents");

            migrationBuilder.AlterColumn<int>(
                name: "organization_id",
                table: "document_type_templates",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_documents_status",
                table: "documents",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_documents_uploaded_at",
                table: "documents",
                column: "uploaded_at");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Document_OrganizationalIntegrity",
                table: "documents",
                sql: "document_type_id IS NULL OR EXISTS (SELECT 1 FROM workspaces w INNER JOIN document_types dt ON dt.organization_id = w.organization_id WHERE w.id = workspace_id AND dt.id = document_type_id)");

            migrationBuilder.AddForeignKey(
                name: "f_k_document_type_templates_organizations_organization_id",
                table: "document_type_templates",
                column: "organization_id",
                principalTable: "organizations",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "f_k_documents_document_types_document_type_id",
                table: "documents",
                column: "document_type_id",
                principalTable: "document_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "f_k_documents_workspaces_workspace_id",
                table: "documents",
                column: "workspace_id",
                principalTable: "workspaces",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "f_k_document_type_templates_organizations_organization_id",
                table: "document_type_templates");

            migrationBuilder.DropForeignKey(
                name: "f_k_documents_document_types_document_type_id",
                table: "documents");

            migrationBuilder.DropForeignKey(
                name: "f_k_documents_workspaces_workspace_id",
                table: "documents");

            migrationBuilder.DropIndex(
                name: "IX_documents_status",
                table: "documents");

            migrationBuilder.DropIndex(
                name: "IX_documents_uploaded_at",
                table: "documents");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Document_OrganizationalIntegrity",
                table: "documents");

            migrationBuilder.AlterColumn<int>(
                name: "organization_id",
                table: "document_type_templates",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddForeignKey(
                name: "f_k_document_type_templates_organizations_organization_id",
                table: "document_type_templates",
                column: "organization_id",
                principalTable: "organizations",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "f_k_documents_document_types_document_type_id",
                table: "documents",
                column: "document_type_id",
                principalTable: "document_types",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "f_k_documents_workspaces_workspace_id",
                table: "documents",
                column: "workspace_id",
                principalTable: "workspaces",
                principalColumn: "id");
        }
    }
}
