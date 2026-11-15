using GoldLedger.Domain.Common;
using GoldLedger.Domain.ValueObjects;
namespace GoldLedger.Tests.Domain.ValueObjects;
public sealed class MoneyTests
{
    [Fact]
    public void Create_with_negative_amount_returns_validation_failure()
    {
        var result = Money.Create(-1m);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }
    [Fact]
    public void Create_with_zero_amount_succeeds()
    {
        var result = Money.Create(0m);
        Assert.True(result.IsSuccess);
        Assert.Equal(0m, result.Value!.Amount);
    }
    [Fact]
    public void Create_rounds_to_two_decimal_places()
    {
        var result = Money.Create(12.345m);
        Assert.True(result.IsSuccess);
        Assert.Equal(12.34m, result.Value!.Amount);
    }
    [Fact]
    public void Addition_returns_sum()
    {
        var firstAmount = Money.Create(1000m).Value!;
        var secondAmount = Money.Create(500m).Value!;
        var totalAmount = firstAmount + secondAmount;
        Assert.Equal(1500m, totalAmount.Amount);
    }
    [Fact]
    public void Subtraction_returns_difference()
    {
        var originalAmount = Money.Create(1000m).Value!;
        var deductedAmount = Money.Create(400m).Value!;
        var remainingAmount = originalAmount - deductedAmount;
        Assert.Equal(600m, remainingAmount.Amount);
    }
    [Fact]
    public void Multiplication_returns_scaled_amount()
    {
        var unitAmount = Money.Create(125m).Value!;
        var scaledAmount = unitAmount * 2.5m;
        Assert.Equal(312.5m, scaledAmount.Amount);
    }
    [Fact]
    public void Division_returns_scaled_amount()
    {
        var totalAmount = Money.Create(100m).Value!;
        var dividedAmount = totalAmount / 4m;
        Assert.Equal(25m, dividedAmount.Amount);
    }
    [Fact]
    public void Percentage_calculation_returns_expected_amount()
    {
        var baseAmount = Money.Create(1000m).Value!;
        var percentage = Percentage.Create(2.5m).Value!;
        var percentageAmount = baseAmount.CalculatePercentage(percentage);
        Assert.Equal(25m, percentageAmount.Amount);
    }
    [Fact]
    public void Equality_uses_amount()
    {
        var firstAmount = Money.Create(100m).Value!;
        var secondAmount = Money.Create(100m).Value!;
        Assert.Equal(firstAmount, secondAmount);
    }
}


