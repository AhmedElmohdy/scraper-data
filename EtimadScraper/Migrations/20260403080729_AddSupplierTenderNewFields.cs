using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EtimadScraper.Migrations
{
    /// <inheritdoc />
    public partial class AddSupplierTenderNewFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CondetionalBookletPrice",
                table: "SupplierTenders",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasInvitations",
                table: "SupplierTenders",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsUGRP",
                table: "SupplierTenders",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastEnqueriesDateHijri",
                table: "SupplierTenders",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastOfferPresentationDateHijri",
                table: "SupplierTenders",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OffersOpeningDateHijri",
                table: "SupplierTenders",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TenderStatusIdString",
                table: "SupplierTenders",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TenderStatusName",
                table: "SupplierTenders",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UgrpRFXResponseURL",
                table: "SupplierTenders",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UgrpRfxUrl",
                table: "SupplierTenders",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CondetionalBookletPrice",
                table: "SupplierTenders");

            migrationBuilder.DropColumn(
                name: "HasInvitations",
                table: "SupplierTenders");

            migrationBuilder.DropColumn(
                name: "IsUGRP",
                table: "SupplierTenders");

            migrationBuilder.DropColumn(
                name: "LastEnqueriesDateHijri",
                table: "SupplierTenders");

            migrationBuilder.DropColumn(
                name: "LastOfferPresentationDateHijri",
                table: "SupplierTenders");

            migrationBuilder.DropColumn(
                name: "OffersOpeningDateHijri",
                table: "SupplierTenders");

            migrationBuilder.DropColumn(
                name: "TenderStatusIdString",
                table: "SupplierTenders");

            migrationBuilder.DropColumn(
                name: "TenderStatusName",
                table: "SupplierTenders");

            migrationBuilder.DropColumn(
                name: "UgrpRFXResponseURL",
                table: "SupplierTenders");

            migrationBuilder.DropColumn(
                name: "UgrpRfxUrl",
                table: "SupplierTenders");
        }
    }
}
