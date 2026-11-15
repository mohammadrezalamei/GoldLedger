using GoldLedger.Domain.Aggregates.GoldOrder;
using GoldLedger.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace GoldLedger.Infrastructure.Persistence.Configurations;
public sealed class GoldOrderConfiguration : IEntityTypeConfiguration<GoldOrder>
{
    public void Configure(EntityTypeBuilder<GoldOrder> builder)
    {
        builder.ToTable("Orders");
        builder.HasKey(order => order.Id);
        builder.Property(order => order.Weight).HasConversion(weight => weight.Value, value => GoldWeight.Create(value).Value!).HasPrecision(18, 6);
        builder.Property(order => order.UnitPrice).HasConversion(money => money.Amount, value => Money.Create(value).Value!).HasPrecision(18, 2);
        builder.Property(order => order.TotalAmount).HasConversion(money => money.Amount, value => Money.Create(value).Value!).HasPrecision(18, 2);
        builder.Property(order => order.FeePercent).HasConversion(percent => percent.Value, value => Percentage.Create(value).Value!).HasPrecision(9, 4);
        builder.Property(order => order.UserNote).HasMaxLength(1000);
        builder.Property(order => order.ConcurrencyToken).IsConcurrencyToken();
        builder.Ignore(order => order.DomainEvents);
        builder.OwnsMany(order => order.Tags, tags =>
        {
            tags.ToTable("OrderTags");
            tags.WithOwner().HasForeignKey("OrderId");
            tags.Property<Guid>("Id");
            tags.HasKey("Id");
            tags.Property(tag => tag.Value).HasMaxLength(100).IsRequired();
        });
    }
}

