using GoldLedger.Domain.Aggregates.GoldOrder;
using GoldLedger.Domain.Aggregates.GoldWallet;
using GoldLedger.Domain.ValueObjects;
using GoldLedger.Domain.Aggregates.Payments;
using Microsoft.EntityFrameworkCore;
namespace GoldLedger.Infrastructure.Persistence;
public sealed class GoldLedgerDbContext(DbContextOptions<GoldLedgerDbContext> options) : DbContext(options)
{
    public DbSet<GoldWallet> Wallets => Set<GoldWallet>();
    public DbSet<GoldOrder> Orders => Set<GoldOrder>();
    public DbSet<PaymentSettlement> PaymentSettlements => Set<PaymentSettlement>();
    public DbSet<DomainEventRecord> DomainEventRecords => Set<DomainEventRecord>();

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
        ChangeTracker.DetectChanges();

        var entries = ChangeTracker.Entries().ToArray();
        var orders = entries
            .Where(entry => entry.Entity is GoldOrder)
            .ToDictionary(entry => ((GoldOrder)entry.Entity).Id);

        foreach (var tagEntry in entries.Where(entry =>
                     entry.Entity is Tag &&
                     entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
        {
            var orderId = (Guid)tagEntry.Property("OrderId").CurrentValue!;
            if (!orders.TryGetValue(orderId, out var orderEntry))
                throw new InvalidOperationException("تغییر تگ باید از طریق سفارش بارگذاری‌شده انجام شود.");

            if (orderEntry.State is EntityState.Unchanged or EntityState.Modified)
                orderEntry.Property(nameof(GoldOrder.ConcurrencyToken)).IsModified = true;
        }

        foreach (var entry in entries.Where(entry => entry.State == EntityState.Modified))
        {
            if (entry.Metadata.FindProperty("ConcurrencyToken") is null)
                continue;

            var token = entry.Property("ConcurrencyToken");
            token.CurrentValue = checked((int)token.OriginalValue! + 1);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(GoldLedgerDbContext).Assembly);
    }
}
