using GoldLedger.Domain.Aggregates.GoldOrder;
using GoldLedger.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace GoldLedger.Infrastructure.Persistence.Configurations;
public sealed class GoldOrderConfiguration : IEntityTypeConfiguration<GoldOrder>
{
    public void Configure(EntityTypeBuilder<GoldOrder> builder)
    {
        builder.ToTable("Orders", table =>
        {
            table.HasCheckConstraint("CK_Orders_Type", "Type IN (1, 2)");
            table.HasCheckConstraint("CK_Orders_Status", "Status IN (1, 2, 3)");
            table.HasCheckConstraint("CK_Orders_Weight", "CAST(Weight AS NUMERIC) > 0");
            table.HasCheckConstraint("CK_Orders_UnitPrice", "CAST(UnitPrice AS NUMERIC) > 0");
            table.HasCheckConstraint("CK_Orders_TotalAmount", "CAST(TotalAmount AS NUMERIC) >= 0");
            table.HasCheckConstraint("CK_Orders_FeePercent", "CAST(FeePercent AS NUMERIC) BETWEEN 0 AND 100");
            table.HasCheckConstraint("CK_Orders_UserNote", "UserNote IS NULL OR length(UserNote) <= 1000");
        });
        builder.Property(order => order.CreatedAt)
            .HasConversion(value => value, value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
        builder.HasKey(order => order.Id);
        builder.HasIndex(order => new { order.UserId, order.CreatedAt });
        builder.Property(order => order.Weight).HasConversion(weight => weight.Value, value => GoldWeight.Create(value).Value!).HasPrecision(18, 6);
        builder.Property(order => order.UnitPrice).HasConversion(money => money.Amount, value => Money.Create(value).Value!).HasPrecision(18, 2);
        builder.Property(order => order.TotalAmount).HasConversion(money => money.Amount, value => Money.Create(value).Value!).HasPrecision(18, 2);
        builder.Property(order => order.FeePercent).HasConversion(percent => percent.Value, value => Percentage.Create(value).Value!).HasPrecision(9, 4);
        builder.Property(order => order.UserNote).HasMaxLength(1000);
        builder.Property(order => order.ConcurrencyToken).IsConcurrencyToken();
        builder.Ignore(order => order.DomainEvents);
        builder.OwnsMany(order => order.Tags, tags =>
        {
            tags.ToTable("OrderTags", table =>
                table.HasCheckConstraint("CK_OrderTags_Value", "length(trim(Value)) BETWEEN 1 AND 50"));
            tags.WithOwner().HasForeignKey("OrderId");
            tags.Property<Guid>("Id");
            tags.HasKey("Id");
            tags.Property(tag => tag.Value).HasMaxLength(Tag.MaxLength).IsRequired();
        });
    }
}
