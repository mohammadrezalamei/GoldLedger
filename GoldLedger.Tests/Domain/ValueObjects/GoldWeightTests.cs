using GoldLedger.Domain.Common;
using GoldLedger.Domain.ValueObjects;
namespace GoldLedger.Tests.Domain.ValueObjects;
public sealed class GoldWeightTests
{
    [Fact]
    public void Create_with_negative_value_fails()
    {
        var result = GoldWeight.Create(-1m);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }
    [Fact]
    public void Create_with_zero_value_succeeds()
    {
        var result = GoldWeight.Create(0m);
        Assert.True(result.IsSuccess);
    }
    [Fact]
    public void Create_with_positive_value_succeeds()
    {
        var result = GoldWeight.Create(1.25m);
        Assert.True(result.IsSuccess);
        Assert.Equal(1.25m, result.Value!.Value);
    }
    [Fact]
    public void Addition_returns_combined_weight()
    {
        var firstWeight = GoldWeight.Create(1m).Value!;
        var secondWeight = GoldWeight.Create(2m).Value!;
        var combinedWeight = firstWeight + secondWeight;
        Assert.Equal(3m, combinedWeight.Value);
    }
    [Fact]
    public void Subtraction_returns_remaining_weight()
    {
        var originalWeight = GoldWeight.Create(5m).Value!;
        var deductedWeight = GoldWeight.Create(2m).Value!;
        var remainingWeight = originalWeight - deductedWeight;
        Assert.Equal(3m, remainingWeight.Value);
    }
    [Fact]
    public void Multiplication_by_money_returns_amount()
    {
        var weight = GoldWeight.Create(2m).Value!;
        var price = Money.Create(1000m).Value!;
        var totalAmount = weight * price;
        Assert.Equal(2000m, totalAmount.Amount);
    }
}


