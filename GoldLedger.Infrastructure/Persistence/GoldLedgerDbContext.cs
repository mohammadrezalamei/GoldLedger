using GoldLedger.Domain.Aggregates.GoldOrder;
using GoldLedger.Domain.Aggregates.GoldWallet;
using Microsoft.EntityFrameworkCore;
namespace GoldLedger.Infrastructure.Persistence;
public sealed class GoldLedgerDbContext(DbContextOptions<GoldLedgerDbContext> options) : DbContext(options)
{
    public DbSet<GoldWallet> Wallets => Set<GoldWallet>();
    public DbSet<GoldOrder> Orders => Set<GoldOrder>();

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        AdvanceConcurrencyTokens();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        AdvanceConcurrencyTokens();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void AdvanceConcurrencyTokens()
    {
        foreach (var entry in ChangeTracker.Entries().Where(entry => entry.State == EntityState.Modified))
        {
            if (entry.Metadata.FindProperty("ConcurrencyToken") is not null)
            {
                var currentToken = (int)(entry.Property("ConcurrencyToken").CurrentValue ?? 0);
                entry.Property("ConcurrencyToken").CurrentValue = currentToken + 1;
            }
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(GoldLedgerDbContext).Assembly);
    }
}

