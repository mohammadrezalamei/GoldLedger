using GoldLedger.Domain.Aggregates.GoldWallet;
using GoldLedger.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace GoldLedger.Infrastructure.Persistence.Configurations;
public sealed class GoldWalletConfiguration : IEntityTypeConfiguration<GoldWallet>
{
    public void Configure(EntityTypeBuilder<GoldWallet> builder)
    {
        builder.ToTable("Wallets", table =>
        {
            table.HasCheckConstraint("CK_Wallets_Balance", "CAST(Balance AS NUMERIC) >= 0");
            table.HasCheckConstraint("CK_Wallets_LockedBalance", "CAST(LockedBalance AS NUMERIC) >= 0 AND CAST(LockedBalance AS NUMERIC) <= CAST(Balance AS NUMERIC)");
        });
        builder.Property(wallet => wallet.CreatedAt)
            .HasConversion(value => value, value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
        builder.Property(wallet => wallet.UpdatedAt)
            .HasConversion(value => value, value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
        builder.HasKey(wallet => wallet.Id);
        builder.HasIndex(wallet => wallet.UserId).IsUnique();
        builder.Property(wallet => wallet.Balance).HasConversion(weight => weight.Value, value => GoldWeight.Create(value).Value!).HasPrecision(18, 6);
        builder.Property(wallet => wallet.LockedBalance).HasConversion(weight => weight.Value, value => GoldWeight.Create(value).Value!).HasPrecision(18, 6);
        builder.Property(wallet => wallet.ConcurrencyToken).IsConcurrencyToken();
        builder.Ignore(wallet => wallet.DomainEvents);
    }
}
