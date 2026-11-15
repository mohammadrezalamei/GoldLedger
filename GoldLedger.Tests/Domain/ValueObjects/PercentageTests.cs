using GoldLedger.Domain.Common;
using GoldLedger.Domain.ValueObjects;
using Xunit;

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

    [Fact]
    public void Percentage_rejects_values_above_one_hundred()
    {
        var result = Percentage.Create(100.01m);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public void Percentage_accepts_zero_and_one_hundred()
    {
        Assert.True(Percentage.Create(0m).IsSuccess);
        Assert.True(Percentage.Create(100m).IsSuccess);
    }

    [Fact]
    public void Percentage_calculates_amount_from_base_value()
    {
        var percentage = Percentage.Create(12.5m).Value!;

        Assert.Equal(25m, percentage.CalculateAmount(200m));
    }

    public static IEnumerable<object[]> InvalidPercentages()
    {
        yield return new object[] { -1m };
        yield return new object[] { -0.01m };
        yield return new object[] { -0.004m };
        yield return new object[] { -0.0001m };
        yield return new object[] { 100.0001m };
        yield return new object[] { 100.004m };
        yield return new object[] { 100.01m };
        yield return new object[] { 101m };
    }

    public static IEnumerable<object[]> ValidPercentages()
    {
        yield return new object[] { 0m, 0m };
        yield return new object[] { 0.004m, 0m };
        yield return new object[] { 0.005m, 0.01m };
        yield return new object[] { 12.344m, 12.34m };
        yield return new object[] { 12.345m, 12.35m };
        yield return new object[] { 99.999m, 100m };
        yield return new object[] { 100m, 100m };
    }

    [Theory]
    [MemberData(nameof(InvalidPercentages))]
    public void Percentage_rejects_out_of_range_input_before_rounding(
        decimal input)
    {
        var result = Percentage.Create(input);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Theory]
    [MemberData(nameof(ValidPercentages))]
    public void Percentage_normalizes_valid_input(
        decimal input,
        decimal expected)
    {
        var result = Percentage.Create(input);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value!.Value);
    }

    [Fact]
    public void Percentage_addition_rejects_result_above_one_hundred()
    {
        var left = Percentage.Create(100m).Value!;
        var right = Percentage.Create(0.01m).Value!;

        Assert.Throws<InvalidOperationException>(() => left + right);
    }

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(20, 30, 50)]
    [InlineData(40, 60, 100)]
    [InlineData(100, 0, 100)]
    public void Percentage_addition_accepts_valid_result(
        int leftValue,
        int rightValue,
        int expected)
    {
        var left = Percentage.Create(leftValue).Value!;
        var right = Percentage.Create(rightValue).Value!;

        Assert.Equal((decimal)expected, (left + right).Value);
    }

    [Fact]
    public void Percentage_subtraction_rejects_negative_result()
    {
        var left = Percentage.Create(10m).Value!;
        var right = Percentage.Create(10.01m).Value!;

        Assert.Throws<InvalidOperationException>(() => left - right);
    }

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(50, 20, 30)]
    [InlineData(100, 100, 0)]
    [InlineData(100, 0, 100)]
    public void Percentage_subtraction_accepts_valid_result(
        int leftValue,
        int rightValue,
        int expected)
    {
        var left = Percentage.Create(leftValue).Value!;
        var right = Percentage.Create(rightValue).Value!;

        Assert.Equal((decimal)expected, (left - right).Value);
    }

    [Fact]
    public void Percentage_arithmetic_preserves_fractional_values()
    {
        var left = Percentage.Create(12.34m).Value!;
        var right = Percentage.Create(0.01m).Value!;

        Assert.Equal(12.35m, (left + right).Value);
        Assert.Equal(12.33m, (left - right).Value);
    }
}
