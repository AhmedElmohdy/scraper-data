using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EtimadScraper.Migrations
{
    /// <inheritdoc />
    public partial class AddSupplierTenderDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SupplierTendersDetialsAwarding",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenderId = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    AwardingResultStatus = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AwardingResultMessage = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ScrapedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierTendersDetialsAwarding", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SupplierTendersDetialsDates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenderId = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    InquiryDeadline = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SubmissionDeadline = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OfferOpeningDate = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TechnicalOfferOpeningDate = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    StopPeriod = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ExpectedAwardDate = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ActionStartDate = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    QuestionSubmissionStartDate = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MaxQuestionResponseTime = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OpeningPlace = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ScrapedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierTendersDetialsDates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SupplierTendersDetialsLocalContent",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenderId = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    LocalContentRequirements = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ScrapedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierTendersDetialsLocalContent", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SupplierTendersDetialsMain",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenderId = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    TenderNumberIAM = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Purpose = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DocumentsValue = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ContractDuration = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MaintenanceInsurance = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CompetitionType = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Organization = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RemainingTime = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SubmissionMethod = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    InitialGuaranteeRequirements = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    InitialGuaranteeTitle = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    InitialGuaranteeValue = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FinalGuarantee = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ScrapedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierTendersDetialsMain", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SupplierTendersDetialsRelations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenderId = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TenderCondition = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ExecutionLocation = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Category = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SupplyItemsIncluded = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ConstructionWorks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MaintenanceAndOperationWorks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ScrapedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierTendersDetialsRelations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SupplierTendersDetialsAwarding_TenderId",
                table: "SupplierTendersDetialsAwarding",
                column: "TenderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupplierTendersDetialsDates_TenderId",
                table: "SupplierTendersDetialsDates",
                column: "TenderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupplierTendersDetialsLocalContent_TenderId",
                table: "SupplierTendersDetialsLocalContent",
                column: "TenderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupplierTendersDetialsMain_TenderId",
                table: "SupplierTendersDetialsMain",
                column: "TenderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupplierTendersDetialsRelations_TenderId",
                table: "SupplierTendersDetialsRelations",
                column: "TenderId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SupplierTendersDetialsAwarding");

            migrationBuilder.DropTable(
                name: "SupplierTendersDetialsDates");

            migrationBuilder.DropTable(
                name: "SupplierTendersDetialsLocalContent");

            migrationBuilder.DropTable(
                name: "SupplierTendersDetialsMain");

            migrationBuilder.DropTable(
                name: "SupplierTendersDetialsRelations");
        }
    }
}
