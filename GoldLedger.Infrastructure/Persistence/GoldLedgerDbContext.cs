using GoldLedger.Domain.Aggregates.GoldOrder;
using GoldLedger.Domain.Aggregates.GoldWallet;
using Microsoft.EntityFrameworkCore;
namespace GoldLedger.Infrastructure.Persistence;
public sealed class GoldLedgerDbContext(DbContextOptions<GoldLedgerDbContext> options) : DbContext(options)
{
    public DbSet<GoldWallet> Wallets => Set<GoldWallet>();
    public DbSet<GoldOrder> Orders => Set<GoldOrder>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(GoldLedgerDbContext).Assembly);
    }
}

