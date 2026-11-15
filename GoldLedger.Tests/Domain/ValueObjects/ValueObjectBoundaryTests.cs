using GoldLedger.Domain.Common;
using GoldLedger.Domain.ValueObjects;
using Xunit;

namespace GoldLedger.Tests.Domain.ValueObjects;

public sealed class ValueObjectBoundaryTests
{
    [Fact]
    public void GoldWeight_rounds_to_three_decimal_places()
    {
        var result = GoldWeight.Create(1.2346m);

        Assert.True(result.IsSuccess);
        Assert.Equal(1.235m, result.Value!.Value);
    }

    [Fact]
    public void GoldWeight_division_by_zero_throws()
    {
        var weight = GoldWeight.Create(1m).Value!;

        Assert.Throws<DivideByZeroException>(() => weight / 0m);
    }

    [Fact]
    public void GoldWeight_negative_multiplier_throws()
    {
        var weight = GoldWeight.Create(1m).Value!;

        Assert.Throws<ArgumentException>(() => weight * -1m);
    }

    [Fact]
    public void GoldWeight_comparison_operators_compare_values()
    {
        var smallerWeight = GoldWeight.Create(1m).Value!;
        var largerWeight = GoldWeight.Create(2m).Value!;

        Assert.True(smallerWeight < largerWeight);
        Assert.True(largerWeight > smallerWeight);
        Assert.True(smallerWeight <= largerWeight);
        Assert.True(largerWeight >= smallerWeight);
    }

    [Fact]
    public void Money_rounds_midpoints_to_even()
    {
        var roundsDown = Money.Create(12.345m);
        var roundsUp = Money.Create(12.355m);

        Assert.True(roundsDown.IsSuccess);
        Assert.True(roundsUp.IsSuccess);
        Assert.Equal(12.34m, roundsDown.Value!.Amount);
        Assert.Equal(12.36m, roundsUp.Value!.Amount);
    }

    [Fact]
    public void Money_rejects_negative_input_before_rounding()
    {
        var result = Money.Create(-0.001m);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public void Money_addition_returns_sum()
    {
        var left = Money.Create(10m).Value!;
        var right = Money.Create(20m).Value!;

        Assert.Equal(30m, (left + right).Amount);
    }

    [Fact]
    public void Money_subtraction_returns_non_negative_difference()
    {
        var left = Money.Create(20m).Value!;
        var right = Money.Create(10m).Value!;

        Assert.Equal(10m, (left - right).Amount);
    }

    [Fact]
    public void Money_subtraction_of_equal_amounts_returns_zero()
    {
        var left = Money.Create(10m).Value!;
        var right = Money.Create(10m).Value!;

        Assert.Equal(0m, (left - right).Amount);
    }

    [Fact]
    public void Money_subtraction_rejects_negative_result()
    {
        var left = Money.Create(10m).Value!;
        var right = Money.Create(20m).Value!;

        Assert.Throws<InvalidOperationException>(() => left - right);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    public void Money_negative_multiplier_throws_in_both_operand_orders(
        int initialAmount)
    {
        var money = Money.Create(initialAmount).Value!;

        Assert.Throws<ArgumentOutOfRangeException>(
            () => money * -1m);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => -1m * money);
    }

    [Fact]
    public void Money_zero_multiplier_returns_zero()
    {
        var money = Money.Create(10m).Value!;

        Assert.Equal(0m, (money * 0m).Amount);
        Assert.Equal(0m, (0m * money).Amount);
    }

    [Fact]
    public void Money_positive_multiplier_works_in_both_operand_orders()
    {
        var money = Money.Create(10m).Value!;

        Assert.Equal(25m, (money * 2.5m).Amount);
        Assert.Equal(25m, (2.5m * money).Amount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    public void Money_negative_divisor_throws(int initialAmount)
    {
        var money = Money.Create(initialAmount).Value!;

        Assert.Throws<ArgumentOutOfRangeException>(
            () => money / -1m);
    }

    [Fact]
    public void Money_division_by_zero_throws()
    {
        var money = Money.Create(10m).Value!;

        Assert.Throws<DivideByZeroException>(() => money / 0m);
    }

    [Fact]
    public void Money_division_by_zero_money_throws()
    {
        var money = Money.Create(10m).Value!;
        var zero = Money.Create(0m).Value!;

        Assert.Throws<DivideByZeroException>(() => money / zero);
    }

    [Fact]
    public void Money_division_by_positive_scalar_returns_money()
    {
        var money = Money.Create(10m).Value!;

        Assert.Equal(2.5m, (money / 4m).Amount);
    }

    [Fact]
    public void Money_division_by_money_returns_ratio()
    {
        var left = Money.Create(10m).Value!;
        var right = Money.Create(4m).Value!;

        Assert.Equal(2.5m, left / right);
    }

    [Fact]
    public void Money_arithmetic_preserves_midpoint_to_even_rounding()
    {
        var one = Money.Create(1m).Value!;
        var three = Money.Create(3m).Value!;

        Assert.Equal(0.12m, (one / 8m).Amount);
        Assert.Equal(0.38m, (three / 8m).Amount);
        Assert.Equal(0.12m, (one * 0.125m).Amount);
        Assert.Equal(0.38m, (three * 0.125m).Amount);
    }

    [Fact]
    public void Money_addition_overflow_throws()
    {
        var maximum = Money.Create(decimal.MaxValue).Value!;
        var one = Money.Create(1m).Value!;

        Assert.Throws<OverflowException>(() => maximum + one);
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

    [Fact]
    public void Tag_rejects_null_value()
    {
        var result = Tag.Create(null!);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public void Tag_normalization_collapses_internal_whitespace()
    {
        var result = Tag.Create("  long    term  ");

        Assert.True(result.IsSuccess);
        Assert.Equal("long term", result.Value!.Value);
    }
}
