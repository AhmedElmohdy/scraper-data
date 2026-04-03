using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EtimadScraper.Migrations
{
    /// <inheritdoc />
    public partial class AddInOutStatusToSupplierTenders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InOutStatus",
                table: "SupplierTenders",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                defaultValue: "pending");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InOutStatus",
                table: "SupplierTenders");
        }
    }
}
