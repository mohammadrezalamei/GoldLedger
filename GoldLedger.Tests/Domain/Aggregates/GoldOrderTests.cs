using GoldLedger.Domain.Aggregates.GoldOrder;
using GoldLedger.Domain.ValueObjects;
using Xunit;

namespace GoldLedger.Tests.Domain.Aggregates;

public class GoldOrderTests
{
    [Fact]
    public void Create_ValidBuyOrder_ShouldSucceedAndCalculateTotalAmountCorrectly()
    {
        var userId = Guid.NewGuid();
        var weight = GoldWeight.Create(10.5m).Value!;
        var unitPrice = Money.Create(50_000_000m, "IRR").Value!;
        var feePercent = Percentage.Create(1.0m).Value!;
        var tags = new List<Tag> { Tag.Create("نوسان_گیری").Value! };

        var result = GoldOrder.Create(userId, OrderType.Buy, weight, unitPrice, feePercent, tags, "خرید تستی");

        Assert.True(result.IsSuccess);
        var order = result.Value!;

        Assert.Equal(userId, order.UserId);
        Assert.Equal(OrderType.Buy, order.Type);
        Assert.Equal(OrderStatus.Pending, order.Status);
        Assert.Equal(530_250_000m, order.TotalAmount.Amount);
        Assert.Equal("IRR", order.TotalAmount.Currency);
        Assert.Single(order.Tags);
    }

    [Fact]
    public void Create_ValidSellOrder_ShouldCalculateTotalAmountWithDeductedFee()
    {
        var userId = Guid.NewGuid();
        var weight = GoldWeight.Create(2.0m).Value!;
        var unitPrice = Money.Create(40_000_000m, "IRR").Value!;
        var feePercent = Percentage.Create(0.5m).Value!;

        var result = GoldOrder.Create(userId, OrderType.Sell, weight, unitPrice, feePercent, Enumerable.Empty<Tag>());

        Assert.True(result.IsSuccess);
        var order = result.Value!;

        Assert.Equal(79_600_000m, order.TotalAmount.Amount);
    }

    [Fact]
    public void Create_WithInvalidOrderType_ShouldFail()
    {
        var userId = Guid.NewGuid();
        var weight = GoldWeight.Create(1.0m).Value!;
        var unitPrice = Money.Create(50_000_000m, "IRR").Value!;
        var feePercent = Percentage.Create(1.0m).Value!;

        var result = GoldOrder.Create(userId, OrderType.None, weight, unitPrice, feePercent, Enumerable.Empty<Tag>());

        Assert.False(result.IsSuccess);
        Assert.Equal("نوع سفارش نامعتبر است", result.Message);
    }

    [Fact]
    public void Complete_PendingOrder_ShouldChangeStatusToSettled()
    {
        var order = GoldOrder.Create(
            Guid.NewGuid(),
            OrderType.Buy,
            GoldWeight.Create(1.0m).Value!,
            Money.Create(10_000_000m, "IRR").Value!,
            Percentage.Create(1.0m).Value!,
            Enumerable.Empty<Tag>()
        ).Value!;

        var result = order.Complete();

        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Settled, order.Status);
    }

    [Fact]
    public void Cancel_NonPendingOrder_ShouldFail()
    {
        var order = GoldOrder.Create(
            Guid.NewGuid(),
            OrderType.Buy,
            GoldWeight.Create(1.0m).Value!,
            Money.Create(10_000_000m, "IRR").Value!,
            Percentage.Create(1.0m).Value!,
            Enumerable.Empty<Tag>()
        ).Value!;

        order.Complete();

        var result = order.Cancel();

        Assert.False(result.IsSuccess);
        Assert.Equal(OrderStatus.Settled, order.Status);
    }

    [Fact]
    public void UpdateMetadata_OnActiveOrder_ShouldUpdateTagsAndNote()
    {
        var order = GoldOrder.Create(
            Guid.NewGuid(),
            OrderType.Buy,
            GoldWeight.Create(1.0m).Value!,
            Money.Create(10_000_000m, "IRR").Value!,
            Percentage.Create(1.0m).Value!,
            new List<Tag> { Tag.Create("تگ_قدیمی").Value! },
            "یادداشت قدیمی"
        ).Value!;

        var newTags = new List<Tag> { Tag.Create("صندوق_طلای_فیزیکی").Value! };

        var result = order.UpdateMetadata(newTags, "یادداشت جدید");

        Assert.True(result.IsSuccess);
        Assert.Equal("یادداشت جدید", order.UserNote);
        Assert.Single(order.Tags);
        Assert.Equal("صندوق_طلای_فیزیکی", order.Tags.First().Value);
    }

    [Fact]
    public void Create_WithEmptyUserId_ShouldFail()
    {
        var weight = GoldWeight.Create(1.0m).Value!;
        var unitPrice = Money.Create(10_000_000m, "IRR").Value!;
        var feePercent = Percentage.Create(1.0m).Value!;

        var result = GoldOrder.Create(Guid.Empty, OrderType.Buy, weight, unitPrice, feePercent, Enumerable.Empty<Tag>());

        Assert.False(result.IsSuccess);
        Assert.Equal("شناسه کاربر نامعتبر است.", result.Message);
    }

    [Fact]
    public void Complete_AlreadySettledOrder_ShouldFail()
    {
        var order = GoldOrder.Create(
            Guid.NewGuid(),
            OrderType.Buy,
            GoldWeight.Create(1.0m).Value!,
            Money.Create(10_000_000m, "IRR").Value!,
            Percentage.Create(1.0m).Value!,
            Enumerable.Empty<Tag>()
        ).Value!;

        order.Complete();

        var result = order.Complete();

        Assert.False(result.IsSuccess);
        Assert.Equal("فقط سفارش‌های معلق قابل تکمیل هستند.", result.Message);
    }

    [Fact]
    public void Cancel_PendingOrder_ShouldChangeStatusToCancelled()
    {
        var order = GoldOrder.Create(
            Guid.NewGuid(),
            OrderType.Buy,
            GoldWeight.Create(1.0m).Value!,
            Money.Create(10_000_000m, "IRR").Value!,
            Percentage.Create(1.0m).Value!,
            Enumerable.Empty<Tag>()
        ).Value!;

        var result = order.Cancel();

        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Cancelled, order.Status);
    }

    [Fact]
    public void UpdateMetadata_OnCancelledOrder_ShouldFail()
    {
        var order = GoldOrder.Create(
            Guid.NewGuid(),
            OrderType.Buy,
            GoldWeight.Create(1.0m).Value!,
            Money.Create(10_000_000m, "IRR").Value!,
            Percentage.Create(1.0m).Value!,
            Enumerable.Empty<Tag>(),
            "یادداشت اولیه"
        ).Value!;

        order.Cancel();

        var newTags = new List<Tag> { Tag.Create("تگ_جدید").Value! };
        var result = order.UpdateMetadata(newTags, "یادداشت ویرایش شده");

        Assert.False(result.IsSuccess);
        Assert.Equal("امکان ویرایش سفارش لغو شده وجود ندارد.", result.Message);
        Assert.Equal("یادداشت اولیه", order.UserNote);
    }

    [Fact]
    public void Complete_CancelledOrder_ShouldFail()
    {
        var order = GoldOrder.Create(
            Guid.NewGuid(),
            OrderType.Buy,
            GoldWeight.Create(1.0m).Value!,
            Money.Create(10_000_000m, "IRR").Value!,
            Percentage.Create(1.0m).Value!,
            Enumerable.Empty<Tag>()
        ).Value!;

        order.Cancel();

        var result = order.Complete();

        Assert.False(result.IsSuccess);
        Assert.Equal("فقط سفارش‌های معلق قابل تکمیل هستند.", result.Message);
    }

    [Fact]
    public void Cancel_AlreadyCancelledOrder_ShouldFail()
    {
        var order = GoldOrder.Create(
            Guid.NewGuid(),
            OrderType.Buy,
            GoldWeight.Create(1.0m).Value!,
            Money.Create(10_000_000m, "IRR").Value!,
            Percentage.Create(1.0m).Value!,
            Enumerable.Empty<Tag>()
        ).Value!;

        order.Cancel();

        var result = order.Cancel();

        Assert.False(result.IsSuccess);
        Assert.Equal("فقط سفارش‌های معلق قابل لغو کردن هستند.", result.Message);
    }

    [Fact]
    public void Create_WithNullWeight_ShouldFail()
    {
        var userId = Guid.NewGuid();
        var unitPrice = Money.Create(50_000_000m, "IRR").Value!;
        var feePercent = Percentage.Create(1.0m).Value!;

        var result = GoldOrder.Create(userId, OrderType.Buy, null!, unitPrice, feePercent, Enumerable.Empty<Tag>());

        Assert.False(result.IsSuccess);
        Assert.Equal("وزن طلا نمی‌تواند خالی باشد.", result.Message);
    }

    [Fact]
    public void Create_WithNullUnitPrice_ShouldFail()
    {
        var userId = Guid.NewGuid();
        var weight = GoldWeight.Create(1.0m).Value!;
        var feePercent = Percentage.Create(1.0m).Value!;

        var result = GoldOrder.Create(userId, OrderType.Buy, weight, null!, feePercent, Enumerable.Empty<Tag>());

        Assert.False(result.IsSuccess);
        Assert.Equal("مظنه قیمت نمی‌تواند خالی باشد.", result.Message);
    }

    [Fact]
    public void Create_WithNullFeePercent_ShouldFail()
    {
        var userId = Guid.NewGuid();
        var weight = GoldWeight.Create(1.0m).Value!;
        var unitPrice = Money.Create(50_000_000m, "IRR").Value!;

        var result = GoldOrder.Create(userId, OrderType.Buy, weight, unitPrice, null!, Enumerable.Empty<Tag>());

        Assert.False(result.IsSuccess);
        Assert.Equal("درصد کارمزد نمی‌تواند خالی باشد.", result.Message);
    }
}