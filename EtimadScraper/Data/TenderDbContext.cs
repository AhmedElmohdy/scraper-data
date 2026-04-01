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
            entity.Property(t => t.TenderTypeName).HasMaxLength(500);
            entity.Property(t => t.LastEnqueriesDate).HasMaxLength(500);
            entity.Property(t => t.LastOfferPresentationDate).HasMaxLength(500);
            entity.Property(t => t.OffersOpeningDate).HasMaxLength(500);
            entity.Property(t => t.SubmitionDate).HasMaxLength(500);
            entity.Property(t => t.CurrentDateTime).HasMaxLength(500);
            entity.Property(t => t.FinancialFees).HasColumnType("decimal(18,2)");
            entity.Property(t => t.InvitationCost).HasColumnType("decimal(18,2)");
            entity.Property(t => t.BuyingCost).HasColumnType("decimal(18,2)");
        });
    }
}
