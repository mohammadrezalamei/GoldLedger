using GoldLedger.Domain.Common;
using GoldLedger.Domain.ValueObjects;

namespace GoldLedger.Tests.Domain.ValueObjects;

public sealed class ValueObjectRawInputValidationTests
{
    public static IEnumerable<object[]> NegativeWeights()
    {
        yield return new object[] { -1m };
        yield return new object[] { -0.001m };
        yield return new object[] { -0.0005m };
        yield return new object[] { -0.0004m };
        yield return new object[] { -0.0001m };
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

    public static IEnumerable<object[]> ValidWeights()
    {
        yield return new object[] { 0m, 0m };
        yield return new object[] { 0.0004m, 0m };
        yield return new object[] { 0.0005m, 0.001m };
        yield return new object[] { 1.2344m, 1.234m };
        yield return new object[] { 1.2345m, 1.235m };
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
    [MemberData(nameof(NegativeWeights))]
    public void GoldWeight_rejects_negative_input_before_rounding(
        decimal input)
    {
        var result = GoldWeight.Create(input);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
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
    [MemberData(nameof(ValidWeights))]
    public void GoldWeight_normalizes_valid_input(
        decimal input,
        decimal expected)
    {
        var result = GoldWeight.Create(input);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value!.Value);
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
}
