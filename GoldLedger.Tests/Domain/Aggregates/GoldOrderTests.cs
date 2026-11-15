using GoldLedger.Domain.Aggregates.GoldOrder;
using GoldLedger.Domain.Common;
using GoldLedger.Domain.ValueObjects;

namespace GoldLedger.Tests.Domain.Aggregates;

public sealed class GoldOrderTests
{
    [Fact]
    public void Create_buy_order_raises_purchase_event()
    {
        var order = CreateOrder(OrderType.Buy, 2m, 100m, 5m, new[] { "investment" });
        var domainEvent = Assert.Single(order.DomainEvents);
        var purchaseEvent = Assert.IsType<GoldPurchasedDomainEvent>(domainEvent);
        Assert.Equal(order.Id, purchaseEvent.OrderId);
        Assert.Equal(order.TotalAmount, purchaseEvent.TotalAmount);
    }

    [Fact]
    public void Create_sell_order_raises_sale_event()
    {
        var order = CreateOrder(OrderType.Sell, 2m, 100m, 5m, Array.Empty<string>());
        var domainEvent = Assert.Single(order.DomainEvents);
        var saleEvent = Assert.IsType<GoldSoldDomainEvent>(domainEvent);
        Assert.Equal(order.Id, saleEvent.OrderId);
        Assert.Equal(order.Weight, saleEvent.Weight);
    }

    [Fact]
    public void Create_preserves_unique_tags_and_note()
    {
        var order = CreateOrder(OrderType.Buy, 1m, 100m, 0m, new[] { "gold", "gold", "desk" }, "memo");
        Assert.Equal(2, order.Tags.Count);
        Assert.Contains(order.Tags, tag => tag.Value == "gold");
        Assert.Equal("memo", order.UserNote);
    }

    [Fact]
    public void UpdateMetadata_replaces_tags_and_note()
    {
        var order = CreateOrder(OrderType.Buy, 1m, 100m, 0m, new[] { "old" }, "old note");
        var result = order.UpdateMetadata(new[] { Tag.Create("new").Value! }, "new note");
        Assert.True(result.IsSuccess);
        Assert.Single(order.Tags);
        Assert.Equal("new", Assert.Single(order.Tags).Value);
        Assert.Equal("new note", order.UserNote);
    }

    [Fact]
    public void UpdateMetadata_allows_empty_tags()
    {
        var order = CreateOrder(OrderType.Buy, 1m, 100m, 0m, new[] { "old" }, "old note");
        var result = order.UpdateMetadata(Array.Empty<Tag>(), null);
        Assert.True(result.IsSuccess);
        Assert.Empty(order.Tags);
        Assert.Null(order.UserNote);
    }

    [Fact]
    public void UpdateMetadata_rejects_long_note_without_mutating_order()
    {
        var order = CreateOrder(OrderType.Buy, 1m, 100m, 0m, new[] { "old" }, "old note");
        var result = order.UpdateMetadata(Array.Empty<Tag>(), new string('x', 1001));
        Assert.False(result.IsSuccess);
        Assert.Single(order.Tags);
        Assert.Equal("old note", order.UserNote);
    }

    [Fact]
    public void UpdateMetadata_rejects_cancelled_order()
    {
        var order = CreateOrder(OrderType.Buy, 1m, 100m, 0m, Array.Empty<string>());
        order.Cancel();
        var result = order.UpdateMetadata(Array.Empty<Tag>(), "new");
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Complete_clears_pending_state_only_once()
    {
        var order = CreateOrder(OrderType.Buy, 1m, 100m, 0m, Array.Empty<string>());
        Assert.True(order.Complete().IsSuccess);
        Assert.False(order.Complete().IsSuccess);
        Assert.Equal(OrderStatus.Settled, order.Status);
    }

    private static GoldOrder CreateOrder(OrderType orderType, decimal weight, decimal unitPrice, decimal fee, IEnumerable<string> tagValues, string? note = null)
    {
        var tags = tagValues.Select(value => Tag.Create(value).Value!).ToArray();
        return GoldOrder.Create(Guid.NewGuid(), orderType, GoldWeight.Create(weight).Value!, Money.Create(unitPrice).Value!, Percentage.Create(fee).Value!, tags, note).Value!;
    }

    [Fact]
    public void Create_buy_order_calculates_fee_and_pending_status()
    {
        var order = CreateOrder(OrderType.Buy, 2m, 1000m, 1m);
        Assert.Equal(OrderStatus.Pending, order.Status);
        Assert.Equal(2020m, order.TotalAmount.Amount);
    }

    [Fact]
    public void Create_sell_order_deducts_fee()
    {
        var order = CreateOrder(OrderType.Sell, 2m, 1000m, 1m);
        Assert.Equal(1980m, order.TotalAmount.Amount);
    }

    [Fact]
    public void Create_with_empty_user_id_fails()
    {
        var result = GoldOrder.Create(Guid.Empty, OrderType.Buy, GoldWeight.Create(1m).Value!, Money.Create(100m).Value!, Percentage.Create(1m).Value!, []);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public void Create_with_none_type_fails()
    {
        var result = GoldOrder.Create(Guid.NewGuid(), OrderType.None, GoldWeight.Create(1m).Value!, Money.Create(100m).Value!, Percentage.Create(1m).Value!, []);
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Create_with_long_note_fails()
    {
        var longNote = new string('x', 1001);
        var result = GoldOrder.Create(Guid.NewGuid(), OrderType.Buy, GoldWeight.Create(1m).Value!, Money.Create(100m).Value!, Percentage.Create(1m).Value!, [], longNote);
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Complete_pending_order_sets_settled_status()
    {
        var order = CreateOrder(OrderType.Buy, 1m, 100m, 1m);
        var result = order.Complete();
        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Settled, order.Status);
    }

    [Fact]
    public void Cancel_pending_order_sets_cancelled_status()
    {
        var order = CreateOrder(OrderType.Buy, 1m, 100m, 1m);
        var result = order.Cancel();
        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Cancelled, order.Status);
    }

    [Fact]
    public void Settled_order_cannot_be_cancelled()
    {
        var order = CreateOrder(OrderType.Buy, 1m, 100m, 1m);
        order.Complete();
        Assert.False(order.Cancel().IsSuccess);
    }

    [Fact]
    public void Cancelled_order_cannot_be_completed()
    {
        var order = CreateOrder(OrderType.Buy, 1m, 100m, 1m);
        order.Cancel();
        Assert.False(order.Complete().IsSuccess);
    }

    private static GoldOrder CreateOrder(OrderType orderType, decimal weightValue, decimal unitPrice, decimal feePercent)
    {
        return GoldOrder.Create(Guid.NewGuid(), orderType, GoldWeight.Create(weightValue).Value!, Money.Create(unitPrice).Value!, Percentage.Create(feePercent).Value!, []).Value!;
    }

    [Fact]
    public void Create_with_empty_user_id_returns_validation_error()
    {
        var result = GoldOrder.Create(
            Guid.Empty,
            OrderType.Buy,
            GoldWeight.Create(1m).Value!,
            Money.Create(100m).Value!,
            Percentage.Create(1m).Value!,
            []);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(99)]
    public void Create_with_invalid_order_type_returns_validation_error(
        int rawType)
    {
        var result = GoldOrder.Create(
            Guid.NewGuid(),
            (OrderType)rawType,
            GoldWeight.Create(1m).Value!,
            Money.Create(100m).Value!,
            Percentage.Create(1m).Value!,
            []);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Theory]
    [InlineData(OrderType.Buy)]
    [InlineData(OrderType.Sell)]
    public void Create_with_zero_weight_returns_validation_error(
        OrderType type)
    {
        var result = GoldOrder.Create(
            Guid.NewGuid(),
            type,
            GoldWeight.Zero,
            Money.Create(100m).Value!,
            Percentage.Create(1m).Value!,
            []);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Theory]
    [InlineData(OrderType.Buy)]
    [InlineData(OrderType.Sell)]
    public void Create_with_weight_rounded_to_zero_returns_validation_error(
        OrderType type)
    {
        var weightResult = GoldWeight.Create(0.0004m);

        Assert.True(weightResult.IsSuccess);
        Assert.Equal(0m, weightResult.Value!.Value);

        var result = GoldOrder.Create(
            Guid.NewGuid(),
            type,
            weightResult.Value!,
            Money.Create(100m).Value!,
            Percentage.Create(1m).Value!,
            []);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Theory]
    [InlineData(OrderType.Buy)]
    [InlineData(OrderType.Sell)]
    public void Create_with_zero_unit_price_returns_validation_error(
        OrderType type)
    {
        var result = GoldOrder.Create(
            Guid.NewGuid(),
            type,
            GoldWeight.Create(1m).Value!,
            Money.Create(0m).Value!,
            Percentage.Create(1m).Value!,
            []);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Theory]
    [InlineData(OrderType.Buy)]
    [InlineData(OrderType.Sell)]
    public void Create_with_unit_price_rounded_to_zero_returns_validation_error(
        OrderType type)
    {
        var priceResult = Money.Create(0.004m);

        Assert.True(priceResult.IsSuccess);
        Assert.Equal(0m, priceResult.Value!.Amount);

        var result = GoldOrder.Create(
            Guid.NewGuid(),
            type,
            GoldWeight.Create(1m).Value!,
            priceResult.Value!,
            Percentage.Create(1m).Value!,
            []);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public void Create_with_null_weight_returns_validation_error()
    {
        var result = GoldOrder.Create(
            Guid.NewGuid(),
            OrderType.Buy,
            null!,
            Money.Create(100m).Value!,
            Percentage.Create(1m).Value!,
            []);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public void Create_with_null_unit_price_returns_validation_error()
    {
        var result = GoldOrder.Create(
            Guid.NewGuid(),
            OrderType.Buy,
            GoldWeight.Create(1m).Value!,
            null!,
            Percentage.Create(1m).Value!,
            []);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public void Create_with_null_fee_percent_returns_validation_error()
    {
        var result = GoldOrder.Create(
            Guid.NewGuid(),
            OrderType.Buy,
            GoldWeight.Create(1m).Value!,
            Money.Create(100m).Value!,
            null!,
            []);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public void Create_with_long_note_returns_validation_error()
    {
        var result = GoldOrder.Create(
            Guid.NewGuid(),
            OrderType.Buy,
            GoldWeight.Create(1m).Value!,
            Money.Create(100m).Value!,
            Percentage.Create(1m).Value!,
            [],
            new string('x', 1001));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public void Create_with_note_at_maximum_length_succeeds()
    {
        var note = new string('x', 1000);

        var result = GoldOrder.Create(
            Guid.NewGuid(),
            OrderType.Buy,
            GoldWeight.Create(1m).Value!,
            Money.Create(100m).Value!,
            Percentage.Create(1m).Value!,
            [],
            note);

        Assert.True(result.IsSuccess);
        Assert.Equal(note, result.Value!.UserNote);
    }

    [Theory]
    [InlineData(OrderType.Buy)]
    [InlineData(OrderType.Sell)]
    public void Create_with_valid_values_succeeds(OrderType type)
    {
        var userId = Guid.NewGuid();

        var result = GoldOrder.Create(
            userId,
            type,
            GoldWeight.Create(1m).Value!,
            Money.Create(100m).Value!,
            Percentage.Create(1m).Value!,
            []);

        Assert.True(result.IsSuccess);

        var order = result.Value!;

        Assert.Equal(userId, order.UserId);
        Assert.Equal(type, order.Type);
        Assert.Equal(OrderStatus.Pending, order.Status);
        Assert.Equal(1m, order.Weight.Value);
        Assert.Equal(100m, order.UnitPrice.Amount);

        Assert.Equal(
            type == OrderType.Buy ? 101m : 99m,
            order.TotalAmount.Amount);
    }

    [Theory]
    [InlineData(OrderType.Buy)]
    [InlineData(OrderType.Sell)]
    public void Create_with_zero_fee_succeeds(OrderType type)
    {
        var result = GoldOrder.Create(
            Guid.NewGuid(),
            type,
            GoldWeight.Create(1m).Value!,
            Money.Create(100m).Value!,
            Percentage.Zero,
            []);

        Assert.True(result.IsSuccess);
        Assert.Equal(100m, result.Value!.TotalAmount.Amount);
    }

    [Fact]
    public void Settled_order_cannot_be_cancelled_returns_validation_failure()
    {
        var order = CreateOrder();

        var completeResult = order.Complete();

        Assert.True(completeResult.IsSuccess);

        var cancelResult = order.Cancel();

        Assert.False(cancelResult.IsSuccess);
        Assert.Equal(ErrorType.Validation, cancelResult.ErrorType);
        Assert.Equal(OrderStatus.Settled, order.Status);
    }

    private static GoldOrder CreateOrder()
    {
        return GoldOrder.Create(
            Guid.NewGuid(),
            OrderType.Buy,
            GoldWeight.Create(1m).Value!,
            Money.Create(100m).Value!,
            Percentage.Create(1m).Value!,
            []).Value!;
    }

    [Theory]
    [InlineData(0.01, false, 0)]
    [InlineData(4.99, false, 0)]
    [InlineData(5, false, 0)]
    [InlineData(5.01, true, 0.01)]
    [InlineData(10, true, 0.01)]
    public void Buy_order_rejects_rounded_zero_and_accepts_smallest_payable_amount(
        decimal price, bool expectedSuccess, decimal expectedAmount)
    {
        var result = GoldOrder.Create(Guid.NewGuid(), OrderType.Buy,
            GoldWeight.Create(0.001m).Value!, Money.Create(price).Value!,
            Percentage.Create(0).Value!, []);

        Assert.Equal(expectedSuccess, result.IsSuccess);
        if (expectedSuccess)
            Assert.Equal(expectedAmount, result.Value!.TotalAmount.Amount);
        else
            Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public void Sale_with_full_fee_can_have_zero_net_proceeds()
    {
        var result = GoldOrder.Create(Guid.NewGuid(), OrderType.Sell,
            GoldWeight.Create(1).Value!, Money.Create(100).Value!,
            Percentage.Create(100).Value!, []);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value!.TotalAmount.Amount);
    }
}
