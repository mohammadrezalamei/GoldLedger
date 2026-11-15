using GoldLedger.Domain.Common;
using GoldLedger.Domain.ValueObjects;
using Xunit;

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

    public static IEnumerable<object[]> NegativeWeights()
    {
        yield return new object[] { -1m };
        yield return new object[] { -0.001m };
        yield return new object[] { -0.0005m };
        yield return new object[] { -0.0004m };
        yield return new object[] { -0.0001m };
    }

    public static IEnumerable<object[]> ValidWeights()
    {
        yield return new object[] { 0m, 0m };
        yield return new object[] { 0.0004m, 0m };
        yield return new object[] { 0.0005m, 0.001m };
        yield return new object[] { 1.2344m, 1.234m };
        yield return new object[] { 1.2345m, 1.235m };
    }

    [Theory]
    [MemberData(nameof(NegativeWeights))]
    public void GoldWeight_rejects_negative_input_before_rounding(
        decimal input)
    {
        var result = GoldWeight.Create(input);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Theory]
    [MemberData(nameof(ValidWeights))]
    public void GoldWeight_normalizes_valid_input(
        decimal input,
        decimal expected)
    {
        var result = GoldWeight.Create(input);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value!.Value);
    }

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
}
