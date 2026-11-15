using GoldLedger.Domain.Aggregates.GoldOrder;
using GoldLedger.Domain.Common;
using GoldLedger.Domain.ValueObjects;

namespace GoldLedger.Tests.Domain.Aggregates;

public sealed class GoldOrderValidationTests
{
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
    public void Settled_order_cannot_be_cancelled()
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
}
