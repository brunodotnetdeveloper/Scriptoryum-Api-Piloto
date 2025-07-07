using Microsoft.EntityFrameworkCore.Migrations;

namespace Scriptoryum.Api.Migrations;

public partial class AddRegulatoryComplianceToInsightCategory : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Update existing records that have 'Regulatory Compliance' to the new enum value
        migrationBuilder.Sql(
            "UPDATE insights SET category = 'RegulatoryCompliance' WHERE category = 'Regulatory Compliance'");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Revert the changes if needed
        migrationBuilder.Sql(
            "UPDATE insights SET category = 'Outro' WHERE category = 'RegulatoryCompliance'");
    }
}