using GoldLedger.Domain.Common;
using GoldLedger.Domain.ValueObjects;
namespace GoldLedger.Tests.Domain.ValueObjects;
public sealed class PercentageTests
{
    [Fact]
    public void Create_with_negative_value_fails()
    {
        var result = Percentage.Create(-1m);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }
    [Fact]
    public void Create_above_one_hundred_fails()
    {
        var result = Percentage.Create(100.01m);
        Assert.False(result.IsSuccess);
    }
    [Fact]
    public void Create_with_boundary_values_succeeds()
    {
        var zeroPercentage = Percentage.Create(0m);
        var fullPercentage = Percentage.Create(100m);
        Assert.True(zeroPercentage.IsSuccess);
        Assert.True(fullPercentage.IsSuccess);
    }
    [Fact]
    public void Calculate_money_returns_expected_value()
    {
        var percentage = Percentage.Create(15m).Value!;
        var amount = Money.Create(200m).Value!;
        var calculatedAmount = percentage.CalculateMoney(amount);
        Assert.Equal(30m, calculatedAmount.Amount);
    }
}

