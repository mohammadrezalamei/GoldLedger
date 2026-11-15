using GoldLedger.Domain.Aggregates.GoldOrder;
using GoldLedger.Domain.Aggregates.Payments;
using GoldLedger.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoldLedger.Infrastructure.Persistence.Configurations;

public sealed class PaymentSettlementConfiguration : IEntityTypeConfiguration<PaymentSettlement>
{
    public void Configure(EntityTypeBuilder<PaymentSettlement> builder)
    {
        builder.ToTable("PaymentSettlements", table =>
        {
            table.HasCheckConstraint("CK_PaymentSettlements_Amount", "CAST(Amount AS NUMERIC) >= 0");
            table.HasCheckConstraint("CK_PaymentSettlements_OrderType", "OrderType IN (1, 2)");
        });
        builder.Property(settlement => settlement.SettledAt)
            .HasConversion(value => value, value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
        builder.HasKey(settlement => settlement.OrderId);
        builder.HasIndex(settlement => settlement.UserId);
        builder.Property(settlement => settlement.Amount)
            .HasConversion(amount => amount.Amount, value => Money.Create(value).Value!)
            .HasPrecision(18, 2);
        builder.HasOne<GoldOrder>().WithOne()
            .HasForeignKey<PaymentSettlement>(settlement => settlement.OrderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
