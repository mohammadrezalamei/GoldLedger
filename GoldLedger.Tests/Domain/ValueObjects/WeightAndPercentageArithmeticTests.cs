using GoldLedger.Domain.ValueObjects;
using Xunit;

namespace GoldLedger.Tests.Domain.ValueObjects;

public sealed class WeightAndPercentageArithmeticTests
{
    [Fact]
    public void GoldWeight_subtraction_rejects_negative_result()
    {
        var left = GoldWeight.Create(1m).Value!;
        var right = GoldWeight.Create(1.001m).Value!;

        Assert.Throws<InvalidOperationException>(() => left - right);
    }

    [Fact]
    public void GoldWeight_subtraction_of_equal_weights_returns_zero()
    {
        var left = GoldWeight.Create(1.25m).Value!;
        var right = GoldWeight.Create(1.25m).Value!;

        Assert.Equal(0m, (left - right).Value);
    }

    [Fact]
    public void GoldWeight_addition_and_subtraction_preserve_precision()
    {
        var left = GoldWeight.Create(1.234m).Value!;
        var right = GoldWeight.Create(0.001m).Value!;

        Assert.Equal(1.235m, (left + right).Value);
        Assert.Equal(1.233m, (left - right).Value);
    }

    [Fact]
    public void GoldWeight_multiplication_rounds_midpoint_away_from_zero()
    {
        var weight = GoldWeight.Create(0.001m).Value!;

        Assert.Equal(0.001m, (weight * 0.5m).Value);
        Assert.Equal(0.001m, (0.5m * weight).Value);
    }

    [Fact]
    public void GoldWeight_division_rounds_midpoint_away_from_zero()
    {
        var weight = GoldWeight.Create(0.001m).Value!;

        Assert.Equal(0.001m, (weight / 2m).Value);
    }

    [Fact]
    public void GoldWeight_zero_multiplier_returns_zero()
    {
        var weight = GoldWeight.Create(2m).Value!;

        Assert.Equal(0m, (weight * 0m).Value);
        Assert.Equal(0m, (0m * weight).Value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void GoldWeight_negative_multiplier_throws_in_both_orders(
        int initialWeight)
    {
        var weight = GoldWeight.Create(initialWeight).Value!;

        Assert.Throws<ArgumentException>(() => weight * -1m);
        Assert.Throws<ArgumentException>(() => -1m * weight);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void GoldWeight_negative_divisor_throws(int initialWeight)
    {
        var weight = GoldWeight.Create(initialWeight).Value!;

        Assert.Throws<ArgumentException>(() => weight / -1m);
    }

    [Fact]
    public void GoldWeight_division_by_zero_weight_throws()
    {
        var weight = GoldWeight.Create(1m).Value!;

        Assert.Throws<DivideByZeroException>(
            () => weight / GoldWeight.Zero);
    }

    [Fact]
    public void GoldWeight_division_by_weight_returns_unrounded_ratio()
    {
        var left = GoldWeight.Create(1m).Value!;
        var right = GoldWeight.Create(8m).Value!;

        Assert.Equal(0.125m, left / right);
    }

    [Fact]
    public void GoldWeight_addition_overflow_throws()
    {
        var maximum = GoldWeight.Create(decimal.MaxValue).Value!;
        var one = GoldWeight.Create(1m).Value!;

        Assert.Throws<OverflowException>(() => maximum + one);
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
