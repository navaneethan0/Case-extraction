using Microsoft.EntityFrameworkCore;
using ECourtScraperApi.Models;

namespace ECourtScraperApi.Data;

public class CaseDbContext : DbContext
{
    public CaseDbContext(DbContextOptions<CaseDbContext> options) : base(options)
    {
    }

    public DbSet<CaseData> Cases => Set<CaseData>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<CaseData>(entity =>
        {
            // Configure CnrNumber as the primary key
            entity.HasKey(c => c.CnrNumber);

            // Configure nested list entities to map to native PostgreSQL JSONB columns
            entity.Property(c => c.Petitioners)
                  .HasColumnType("jsonb");

            entity.Property(c => c.Respondents)
                  .HasColumnType("jsonb");

            entity.Property(c => c.Acts)
                  .HasColumnType("jsonb");

            entity.Property(c => c.Hearings)
                  .HasColumnType("jsonb");

            entity.Property(c => c.Orders)
                  .HasColumnType("jsonb");

            entity.Property(c => c.Processes)
                  .HasColumnType("jsonb");

            entity.Property(c => c.TransferDetails)
                  .HasColumnType("jsonb");

            entity.Property(c => c.IAStatuses)
                  .HasColumnType("jsonb");
        });
    }
}
