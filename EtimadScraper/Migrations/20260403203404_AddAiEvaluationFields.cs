using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EtimadScraper.Migrations
{
    /// <inheritdoc />
    public partial class AddAiEvaluationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Evaluated",
                table: "SupplierTenders",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MatchingReason",
                table: "SupplierTenders",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MatchingScore",
                table: "SupplierTenders",
                type: "decimal(5,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Evaluated",
                table: "SupplierTenders");

            migrationBuilder.DropColumn(
                name: "MatchingReason",
                table: "SupplierTenders");

            migrationBuilder.DropColumn(
                name: "MatchingScore",
                table: "SupplierTenders");
        }
    }
}
