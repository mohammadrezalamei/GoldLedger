using GoldLedger.Domain.Common;
using GoldLedger.Domain.ValueObjects;
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
    public void Money_rounds_midpoint_away_from_zero()
    {
        var result = Money.Create(12.345m);
        Assert.True(result.IsSuccess);
        Assert.Equal(12.34m, result.Value!.Amount);
    }

    [Fact]
    public void Money_division_by_zero_throws()
    {
        var amount = Money.Create(10m).Value!;
        Assert.Throws<DivideByZeroException>(() => amount / 0m);
    }

    [Fact]
    public void Money_subtraction_returns_difference()
    {
        var amount = Money.Create(10m).Value!;
        var difference = amount - Money.Create(20m).Value!;
        Assert.Equal(-10m, difference.Amount);
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
