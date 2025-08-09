using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Scriptoryum.Api.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDataModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_document_chunks_documents_document_id",
                schema: "public",
                table: "document_chunks");

            migrationBuilder.DropPrimaryKey(
                name: "PK_document_chunks",
                schema: "public",
                table: "document_chunks");

            migrationBuilder.RenameIndex(
                name: "IX_document_chunks_document_id",
                schema: "public",
                table: "document_chunks",
                newName: "i_x_document_chunks_document_id");

            migrationBuilder.AddPrimaryKey(
                name: "p_k_document_chunks",
                schema: "public",
                table: "document_chunks",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "f_k_document_chunks_documents_document_id",
                schema: "public",
                table: "document_chunks",
                column: "document_id",
                principalTable: "documents",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "f_k_document_chunks_documents_document_id",
                schema: "public",
                table: "document_chunks");

            migrationBuilder.DropPrimaryKey(
                name: "p_k_document_chunks",
                schema: "public",
                table: "document_chunks");

            migrationBuilder.RenameIndex(
                name: "i_x_document_chunks_document_id",
                schema: "public",
                table: "document_chunks",
                newName: "IX_document_chunks_document_id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_document_chunks",
                schema: "public",
                table: "document_chunks",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_document_chunks_documents_document_id",
                schema: "public",
                table: "document_chunks",
                column: "document_id",
                principalTable: "documents",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
