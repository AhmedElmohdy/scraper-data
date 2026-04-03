using EtimadScraper.Entities;
using Microsoft.EntityFrameworkCore;

namespace EtimadScraper.Data;

public class TenderDbContext : DbContext
{
    public TenderDbContext(DbContextOptions<TenderDbContext> options) : base(options) { }

    /// <summary>Tenders scraped via Playwright HTML scraping (legacy).</summary>
    public DbSet<TenderEntity> Tenders => Set<TenderEntity>();

    /// <summary>Tenders fetched from the Etimad supplier-tenders JSON API.</summary>
    public DbSet<SupplierTenderEntity> SupplierTenders => Set<SupplierTenderEntity>();

    // ?? Tender details tables (one row per TenderId per section) ??????????????
    public DbSet<SupplierTendersDetialsMain> SupplierTendersDetialsMain => Set<SupplierTendersDetialsMain>();
    public DbSet<SupplierTendersDetialsDates> SupplierTendersDetialsDates => Set<SupplierTendersDetialsDates>();
    public DbSet<SupplierTendersDetialsRelations> SupplierTendersDetialsRelations => Set<SupplierTendersDetialsRelations>();
    public DbSet<SupplierTendersDetialsAwarding> SupplierTendersDetialsAwarding => Set<SupplierTendersDetialsAwarding>();
    public DbSet<SupplierTendersDetialsLocalContent> SupplierTendersDetialsLocalContent => Set<SupplierTendersDetialsLocalContent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<TenderEntity>(entity =>
        {
            entity.ToTable("Tenders");

            entity.HasKey(t => t.Id);

            entity.Property(t => t.TenderNumber)
                  .IsRequired()
                  .HasMaxLength(500);

            entity.Property(t => t.Title).HasMaxLength(2000);
            entity.Property(t => t.Organization).HasMaxLength(1000);
            entity.Property(t => t.PublishDate).HasMaxLength(500);
            entity.Property(t => t.ClosingDate).HasMaxLength(500);
            entity.Property(t => t.DetailsUrl).HasMaxLength(2000);
            entity.Property(t => t.Category).HasMaxLength(1000);
            entity.Property(t => t.Department).HasMaxLength(1000);
            entity.Property(t => t.Status).HasMaxLength(500);
            entity.Property(t => t.AdditionalInfo).HasMaxLength(4000);

            // Unique index on the business key – prevents duplicate inserts
            // and makes the upsert lookup O(log n).
            entity.HasIndex(t => t.TenderNumber)
                  .IsUnique()
                  .HasDatabaseName("IX_Tenders_TenderNumber");
        });

        // -----------------------------------------------------------------------
        // Tender details tables
        // -----------------------------------------------------------------------

        ConfigureDetailsMain(modelBuilder);
        ConfigureDetailsDates(modelBuilder);
        ConfigureDetailsRelations(modelBuilder);
        ConfigureDetailsAwarding(modelBuilder);
        ConfigureDetailsLocalContent(modelBuilder);

        // -----------------------------------------------------------------------
        // SupplierTenderEntity – fetched from the Etimad JSON API
        // -----------------------------------------------------------------------
        modelBuilder.Entity<SupplierTenderEntity>(entity =>
        {
            entity.ToTable("SupplierTenders");

            entity.HasKey(t => t.Id);

            // Unique index on the API's native tender ID – used for upsert deduplication.
            entity.HasIndex(t => t.TenderId)
                  .IsUnique()
                  .HasDatabaseName("IX_SupplierTenders_TenderId");

            entity.Property(t => t.ReferenceNumber).HasMaxLength(500);
            entity.Property(t => t.TenderName).HasMaxLength(2000);
            entity.Property(t => t.TenderNumber).HasMaxLength(500);
            entity.Property(t => t.BranchName).HasMaxLength(1000);
            entity.Property(t => t.AgencyName).HasMaxLength(1000);
            entity.Property(t => t.TenderIdString).HasMaxLength(500);
            entity.Property(t => t.TenderStatusName).HasMaxLength(500);
            entity.Property(t => t.TenderStatusIdString).HasMaxLength(500);
            entity.Property(t => t.TenderTypeName).HasMaxLength(500);
            entity.Property(t => t.LastEnqueriesDate).HasMaxLength(500);
            entity.Property(t => t.LastOfferPresentationDate).HasMaxLength(500);
            entity.Property(t => t.OffersOpeningDate).HasMaxLength(500);
            entity.Property(t => t.SubmitionDate).HasMaxLength(500);
            entity.Property(t => t.LastEnqueriesDateHijri).HasMaxLength(500);
            entity.Property(t => t.OffersOpeningDateHijri).HasMaxLength(500);
            entity.Property(t => t.LastOfferPresentationDateHijri).HasMaxLength(500);
            entity.Property(t => t.CurrentDateTime).HasMaxLength(500);
            entity.Property(t => t.FinancialFees).HasColumnType("decimal(18,2)");
            entity.Property(t => t.InvitationCost).HasColumnType("decimal(18,2)");
            entity.Property(t => t.BuyingCost).HasColumnType("decimal(18,2)");
            entity.Property(t => t.CondetionalBookletPrice).HasColumnType("decimal(18,2)");
            entity.Property(t => t.UgrpRfxUrl).HasMaxLength(2000);
            entity.Property(t => t.UgrpRFXResponseURL).HasMaxLength(2000);
        });
    }

    // ?? Details table configurations ?????????????????????????????????????????

    private static void ConfigureDetailsMain(ModelBuilder mb)
    {
        mb.Entity<SupplierTendersDetialsMain>(e =>
        {
            e.ToTable("SupplierTendersDetialsMain");
            e.HasKey(x => x.Id);
            e.Property(x => x.TenderId).IsRequired().HasMaxLength(500);
            e.HasIndex(x => x.TenderId).IsUnique()
             .HasDatabaseName("IX_SupplierTendersDetialsMain_TenderId");
            e.Property(x => x.Title).HasMaxLength(2000);
            e.Property(x => x.TenderNumberIAM).HasMaxLength(500);
            e.Property(x => x.ReferenceNumber).HasMaxLength(500);
            e.Property(x => x.Purpose).HasMaxLength(2000);
            e.Property(x => x.DocumentsValue).HasMaxLength(500);
            e.Property(x => x.Status).HasMaxLength(500);
            e.Property(x => x.ContractDuration).HasMaxLength(500);
            e.Property(x => x.MaintenanceInsurance).HasMaxLength(500);
            e.Property(x => x.CompetitionType).HasMaxLength(500);
            e.Property(x => x.Organization).HasMaxLength(1000);
            e.Property(x => x.RemainingTime).HasMaxLength(500);
            e.Property(x => x.SubmissionMethod).HasMaxLength(500);
            e.Property(x => x.InitialGuaranteeRequirements).HasMaxLength(500);
            e.Property(x => x.InitialGuaranteeTitle).HasMaxLength(500);
            e.Property(x => x.InitialGuaranteeValue).HasMaxLength(500);
            e.Property(x => x.FinalGuarantee).HasMaxLength(500);
        });
    }

    private static void ConfigureDetailsDates(ModelBuilder mb)
    {
        mb.Entity<SupplierTendersDetialsDates>(e =>
        {
            e.ToTable("SupplierTendersDetialsDates");
            e.HasKey(x => x.Id);
            e.Property(x => x.TenderId).IsRequired().HasMaxLength(500);
            e.HasIndex(x => x.TenderId).IsUnique()
             .HasDatabaseName("IX_SupplierTendersDetialsDates_TenderId");
            e.Property(x => x.InquiryDeadline).HasMaxLength(500);
            e.Property(x => x.SubmissionDeadline).HasMaxLength(500);
            e.Property(x => x.OfferOpeningDate).HasMaxLength(500);
            e.Property(x => x.TechnicalOfferOpeningDate).HasMaxLength(500);
            e.Property(x => x.StopPeriod).HasMaxLength(500);
            e.Property(x => x.ExpectedAwardDate).HasMaxLength(500);
            e.Property(x => x.ActionStartDate).HasMaxLength(500);
            e.Property(x => x.QuestionSubmissionStartDate).HasMaxLength(500);
            e.Property(x => x.MaxQuestionResponseTime).HasMaxLength(500);
            e.Property(x => x.OpeningPlace).HasMaxLength(1000);
        });
    }

    private static void ConfigureDetailsRelations(ModelBuilder mb)
    {
        mb.Entity<SupplierTendersDetialsRelations>(e =>
        {
            e.ToTable("SupplierTendersDetialsRelations");
            e.HasKey(x => x.Id);
            e.Property(x => x.TenderId).IsRequired().HasMaxLength(500);
            e.HasIndex(x => x.TenderId).IsUnique()
             .HasDatabaseName("IX_SupplierTendersDetialsRelations_TenderId");
            e.Property(x => x.TenderCondition).HasMaxLength(1000);
            e.Property(x => x.ExecutionLocation).HasMaxLength(1000);
            e.Property(x => x.Description).HasMaxLength(4000);
            e.Property(x => x.Category).HasMaxLength(1000);
            e.Property(x => x.SupplyItemsIncluded).HasMaxLength(500);
            e.Property(x => x.ConstructionWorks).HasMaxLength(500);
            e.Property(x => x.MaintenanceAndOperationWorks).HasMaxLength(500);
        });
    }

    private static void ConfigureDetailsAwarding(ModelBuilder mb)
    {
        mb.Entity<SupplierTendersDetialsAwarding>(e =>
        {
            e.ToTable("SupplierTendersDetialsAwarding");
            e.HasKey(x => x.Id);
            e.Property(x => x.TenderId).IsRequired().HasMaxLength(500);
            e.HasIndex(x => x.TenderId).IsUnique()
             .HasDatabaseName("IX_SupplierTendersDetialsAwarding_TenderId");
            e.Property(x => x.AwardingResultStatus).HasMaxLength(500);
            e.Property(x => x.AwardingResultMessage).HasMaxLength(4000);
        });
    }

    private static void ConfigureDetailsLocalContent(ModelBuilder mb)
    {
        mb.Entity<SupplierTendersDetialsLocalContent>(e =>
        {
            e.ToTable("SupplierTendersDetialsLocalContent");
            e.HasKey(x => x.Id);
            e.Property(x => x.TenderId).IsRequired().HasMaxLength(500);
            e.HasIndex(x => x.TenderId).IsUnique()
             .HasDatabaseName("IX_SupplierTendersDetialsLocalContent_TenderId");
            e.Property(x => x.LocalContentRequirements).HasMaxLength(4000);
        });
    }
}
