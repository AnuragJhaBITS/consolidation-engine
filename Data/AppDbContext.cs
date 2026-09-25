using Microsoft.EntityFrameworkCore;
using ConsolidationEngine.Models;

namespace ConsolidationEngine.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Subsidiary> Subsidiaries => Set<Subsidiary>();
    public DbSet<TrialBalanceLine> TrialBalanceLines => Set<TrialBalanceLine>();
    public DbSet<ExchangeRate> ExchangeRates => Set<ExchangeRate>();
    public DbSet<IntercompanyTransaction> IntercompanyTransactions => Set<IntercompanyTransaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<IntercompanyTransaction>()
            .HasOne(t => t.FromSubsidiary)
            .WithMany()
            .HasForeignKey(t => t.FromSubsidiaryId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<IntercompanyTransaction>()
            .HasOne(t => t.ToSubsidiary)
            .WithMany()
            .HasForeignKey(t => t.ToSubsidiaryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
