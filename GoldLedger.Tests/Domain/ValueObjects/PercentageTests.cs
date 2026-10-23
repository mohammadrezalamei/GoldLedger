using GoldLedger.Domain.Common;
using GoldLedger.Domain.ValueObjects;
using Xunit;

namespace GoldLedger.Tests.Domain.ValueObjects;

public class PercentageTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(12.5)]
    [InlineData(50)]
    [InlineData(100)]
    public void Create_WithValidValue_ShouldReturnSuccessResult(decimal value)
    {
        var result = Percentage.Create(value);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(value, result.Value.Value);
        Assert.Equal(ErrorType.None, result.ErrorType);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(-5)]
    [InlineData(100.01)]
    [InlineData(150)]
    public void Create_WithInvalidValue_ShouldReturnValidationFailure(decimal value)
    {
        var result = Percentage.Create(value);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Value);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.False(string.IsNullOrWhiteSpace(result.Message));
    }

    [Fact]
    public void Create_ShouldRoundToTwoDecimalPlaces_AwayFromZero()
    {
        var result = Percentage.Create(15.556m);

        Assert.True(result.IsSuccess);
        Assert.Equal(15.56m, result.Value!.Value);
    }

    [Fact]
    public void CalculateAmount_ShouldReturnCorrectCalculatedValue()
    {
        var percentage = Percentage.Create(15m).Value!;
        decimal baseAmount = 2000m;

        var result = percentage.CalculateAmount(baseAmount);

        Assert.Equal(300m, result);
    }

    [Fact]
    public void Equality_WithSameValues_ShouldBeEqual()
    {
        var percentage1 = Percentage.Create(25.5m).Value!;
        var percentage2 = Percentage.Create(25.5m).Value!;

        Assert.Equal(percentage1, percentage2);
        Assert.True(percentage1 == percentage2);
        Assert.False(percentage1 != percentage2);
        Assert.Equal(percentage1.GetHashCode(), percentage2.GetHashCode());
    }

    [Fact]
    public void Equality_WithDifferentValues_ShouldNotBeEqual()
    {
        var percentage1 = Percentage.Create(20m).Value!;
        var percentage2 = Percentage.Create(30m).Value!;

        Assert.NotEqual(percentage1, percentage2);
        Assert.True(percentage1 != percentage2);
        Assert.False(percentage1 == percentage2);
    }

    [Fact]
    public void Operators_AdditionAndSubtraction_ShouldWorkCorrectly()
    {
        var p1 = Percentage.Create(30m).Value!;
        var p2 = Percentage.Create(10m).Value!;

        var sum = p1 + p2;
        var diff = p1 - p2;

        Assert.Equal(40m, sum.Value);
        Assert.Equal(20m, diff.Value);
    }

    [Fact]
    public void Operators_Comparison_ShouldWorkCorrectly()
    {
        var p1 = Percentage.Create(40m).Value!;
        var p2 = Percentage.Create(20m).Value!;

        Assert.True(p1 > p2);
        Assert.True(p2 < p1);
        Assert.True(p1 >= p2);
        Assert.True(p2 <= p1);
    }

    [Fact]
    public void Zero_Constant_ShouldBeZero()
    {
        Assert.Equal(0m, Percentage.Zero.Value);
    }

    [Fact]
    public void CalculateMoney_ShouldReturnCorrectMoneyObject()
    {
        var percentage = Percentage.Create(10m).Value!;
        var money = Money.Create(5000m, "IRR").Value!;

        var result = percentage.CalculateMoney(money);

        Assert.NotNull(result);
        Assert.Equal(500m, result.Amount);
        Assert.Equal("IRR", result.Currency);
    }

    [Fact]
    public void ToString_ShouldReturnFormattedString()
    {
        var percentage = Percentage.Create(12.5m).Value!;

        var str = percentage.ToString();

        Assert.Equal("12.50%", str);
    }

    [Fact]
    public void CalculateMoney_WithNull_ShouldThrowArgumentNullException()
    {
        var percentage = Percentage.Create(10m).Value!;

        Assert.Throws<ArgumentNullException>(() => percentage.CalculateMoney(null!));
    }

    [Theory]
    [InlineData(0, 1000, 0)]
    [InlineData(100, 1000, 1000)]
    public void CalculateAmount_AtBoundaries_ShouldReturnExpected(decimal percentVal, decimal baseAmount, decimal expected)
    {
        var percentage = Percentage.Create(percentVal).Value!;

        var result = percentage.CalculateAmount(baseAmount);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Equals_WithNullOrDifferentType_ShouldReturnFalse()
    {
        var percentage = Percentage.Create(15m).Value!;

        Assert.False(percentage.Equals(null));
        Assert.False(percentage.Equals(new object()));
    }
}