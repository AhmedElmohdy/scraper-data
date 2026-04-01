using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EtimadScraper.Migrations
{
    /// <inheritdoc />
    public partial class AddSupplierTenders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SupplierTenders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenderId = table.Column<int>(type: "int", nullable: false),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TenderName = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    TenderNumber = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    BranchName = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    AgencyName = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TenderIdString = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TenderStatusId = table.Column<int>(type: "int", nullable: true),
                    TenderTypeId = table.Column<int>(type: "int", nullable: true),
                    TenderTypeName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LastEnqueriesDate = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LastOfferPresentationDate = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OffersOpeningDate = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SubmitionDate = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TenderActivityId = table.Column<int>(type: "int", nullable: true),
                    FinancialFees = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    InvitationCost = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    BuyingCost = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    RemainingDays = table.Column<int>(type: "int", nullable: true),
                    RemainingHours = table.Column<int>(type: "int", nullable: true),
                    RemainingMins = table.Column<int>(type: "int", nullable: true),
                    CurrentDateTime = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FirstSyncedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastSyncedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierTenders", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SupplierTenders_TenderId",
                table: "SupplierTenders",
                column: "TenderId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SupplierTenders");
        }
    }
}
