using EtimadScraper.Entities;
using Microsoft.EntityFrameworkCore;

namespace EtimadScraper.Data;

public class TenderDbContext : DbContext
{
    public TenderDbContext(DbContextOptions<TenderDbContext> options) : base(options) { }

    public DbSet<TenderEntity> Tenders => Set<TenderEntity>();

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
    }
}
