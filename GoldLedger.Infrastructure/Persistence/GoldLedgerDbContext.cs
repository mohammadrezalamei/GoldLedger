using GoldLedger.Domain.Aggregates.GoldOrder;
using GoldLedger.Domain.Aggregates.GoldWallet;
using GoldLedger.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace GoldLedger.Infrastructure.Persistence;

public sealed class GoldLedgerDbContext(DbContextOptions<GoldLedgerDbContext> options) : DbContext(options)
{
    public DbSet<GoldWallet> Wallets => Set<GoldWallet>();
    public DbSet<GoldOrder> Orders => Set<GoldOrder>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<GoldWallet>(builder =>
        {
            builder.ToTable("Wallets", table =>
            {
                table.HasCheckConstraint("CK_Wallets_Balance", "Balance >= 0");
                table.HasCheckConstraint("CK_Wallets_LockedBalance", "LockedBalance >= 0 AND LockedBalance <= Balance");
            });
            builder.HasKey(x => x.Id);
            builder.HasIndex(x => x.UserId).IsUnique();
            builder.Property(x => x.Balance).HasConversion(x => x.Value, x => GoldWeight.Create(x).Value!).HasPrecision(18, 6);
            builder.Property(x => x.LockedBalance).HasConversion(x => x.Value, x => GoldWeight.Create(x).Value!).HasPrecision(18, 6);
            builder.Property(x => x.ConcurrencyToken).IsConcurrencyToken();
            builder.Ignore(x => x.DomainEvents);
        });

        modelBuilder.Entity<GoldOrder>(builder =>
        {
            builder.ToTable("Orders");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Weight).HasConversion(x => x.Value, x => GoldWeight.Create(x).Value!).HasPrecision(18, 6);
            builder.Property(x => x.UnitPrice).HasConversion(x => x.Amount, x => Money.Create(x, "IRR").Value!).HasPrecision(18, 2);
            builder.Property(x => x.TotalAmount).HasConversion(x => x.Amount, x => Money.Create(x, "IRR").Value!).HasPrecision(18, 2);
            builder.Property(x => x.FeePercent).HasConversion(x => x.Value, x => Percentage.Create(x).Value!).HasPrecision(9, 4);
            builder.Property(x => x.UserNote).HasMaxLength(1000);
            builder.Property(x => x.ConcurrencyToken).IsConcurrencyToken();
            builder.Ignore(x => x.DomainEvents);
            builder.OwnsMany(x => x.Tags, tags =>
            {
                tags.ToTable("OrderTags");
                tags.WithOwner().HasForeignKey("OrderId");
                tags.Property<Guid>("Id");
                tags.HasKey("Id");
                tags.Property(x => x.Value).HasMaxLength(100).IsRequired();
            });
        });
    }
}
