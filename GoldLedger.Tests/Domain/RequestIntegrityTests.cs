using GoldLedger.Domain.Aggregates.GoldOrder;
using GoldLedger.Domain.Common;
using GoldLedger.Domain.ValueObjects;

namespace GoldLedger.Tests.Domain;

public sealed class RequestIntegrityTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("key with space")]
    [InlineData("کلید")]
    [InlineData("key/part")]
    [InlineData("key,other")]
    [InlineData("key\n")]
    public void Invalid_request_key_is_rejected_in_domain(string? value)
    {
        var result = IdempotencyKey.Create(value);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Theory]
    [InlineData("a")]
    [InlineData("Order-123_test.v1")]
    public void Valid_request_key_preserves_value(string value)
    {
        var result = IdempotencyKey.Create(value);

        Assert.True(result.IsSuccess);
        Assert.Equal(value, result.Value!.Value);
    }

    [Fact]
    public void Request_key_accepts_maximum_length_and_rejects_one_more_character()
    {
        Assert.True(IdempotencyKey.Create(new string('a', 128)).IsSuccess);
        Assert.False(IdempotencyKey.Create(new string('a', 129)).IsSuccess);
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
