using GoldLedger.Domain.ValueObjects;
using GoldLedger.Domain.Common;

namespace GoldLedger.Tests.Domain.ValueObjects;

public class GoldWeightTests
{
    [Theory]
    [InlineData(12.4567, 12.457)]
    [InlineData(10.1234, 10.123)]
    [InlineData(12.4585, 12.459)]
    [InlineData(12.4584, 12.458)]
    [InlineData(5.5, 5.500)]
    public void Create_WithValidDecimal_ShouldReturnSuccessAndRoundedValue(decimal input, decimal expected)
    {
        var result = GoldWeight.Create(input);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(expected, result.Value.Value);
        Assert.Equal(ErrorType.None, result.ErrorType);
    }

    [Fact]
    public void Create_WithNegativeValue_ShouldReturnFailureWithValidationError()
    {
        decimal inputWeight = -2.5m;

        var result = GoldWeight.Create(inputWeight);

        Assert.True(!result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public void Addition_WithTwoValidWeights_ShouldReturnCorrectSum()
    {
        var weight1 = GoldWeight.Create(10.500m).Value!;
        var weight2 = GoldWeight.Create(5.250m).Value!;

        var sum = weight1 + weight2;

        Assert.NotNull(sum);
        Assert.Equal(15.750m, sum.Value);
    }

    [Fact]
    public void Subtraction_WithValidWeights_ShouldReturnCorrectDifference()
    {
        var weight1 = GoldWeight.Create(20.000m).Value!;
        var weight2 = GoldWeight.Create(7.500m).Value!;

        var difference = weight1 - weight2;

        Assert.NotNull(difference);
        Assert.Equal(12.500m, difference.Value);
    }

    [Fact]
    public void Equals_WithSameValues_ShouldBeEqual()
    {
        var weight1 = GoldWeight.Create(15.234m).Value!;
        var weight2 = GoldWeight.Create(15.234m).Value!;

        Assert.True(weight1 == weight2);
        Assert.True(weight1.Equals(weight2));
        Assert.Equal(weight1.GetHashCode(), weight2.GetHashCode());
    }

    [Fact]
    public void Equals_WithDifferentValues_ShouldNotBeEqual()
    {
        var weight1 = GoldWeight.Create(15.234m).Value!;
        var weight2 = GoldWeight.Create(10.000m).Value!;

        Assert.True(weight1 != weight2);
        Assert.False(weight1.Equals(weight2));
    }

    [Fact]
    public void Create_WithZeroValue_ShouldReturnSuccess()
    {
        var result = GoldWeight.Create(0m);

        Assert.True(result.IsSuccess);
        Assert.Equal(0.000m, result.Value!.Value);
    }

    [Theory]
    [InlineData(12.4567, 12.457)]
    [InlineData(10.1234, 10.123)]
    [InlineData(12.4585, 12.459)]
    [InlineData(12.4584, 12.458)]
    [InlineData(5.5, 5.500)]
    public void Create_WithMidpointValues_ShouldRoundCorrectly(decimal input, decimal expected)
    {
        var result = GoldWeight.Create(input);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value!.Value);
    }

    [Fact]
    public void Operators_WithNullOperands_ShouldHandleGracefully()
    {
        GoldWeight? weight1 = null;
        var weight2 = GoldWeight.Create(10m).Value;

        Assert.True(weight1 == null);
        Assert.False(weight2 == null);
    }

    [Fact]
    public void Equals_WithNull_ShouldReturnFalse()
    {
        var weight = GoldWeight.Create(10m).Value!;

        Assert.False(weight.Equals(null));
    }

    [Fact]
    public void ComparisonOperators_ShouldWorkCorrectly()
    {
        var lighter = GoldWeight.Create(5.000m).Value!;
        var heavier = GoldWeight.Create(10.500m).Value!;
        var duplicate = GoldWeight.Create(5.000m).Value!;

        Assert.True(lighter < heavier);
        Assert.True(heavier > lighter);
        Assert.True(lighter <= duplicate);
        Assert.True(lighter >= duplicate);
        Assert.False(heavier < lighter);
    }
    [Fact]
    public void Multiplication_GoldWeightAndMoney_ShouldReturnCorrectMoneyInBothDirections()
    {
        var weight = GoldWeight.Create(2.000m).Value!;
        var money = Money.Create(100000m, "IRR").Value!;

        var result1 = weight * money;
        var result2 = money * weight;

        Assert.Equal(200000m, result1.Amount);
        Assert.Equal("IRR", result1);
        Assert.Equal(200000m, result2.Amount);
    }

    [Fact]
    public void Multiplication_GoldWeightAndScalar_ShouldReturnCorrectWeight()
    {
        var weight = GoldWeight.Create(5.000m).Value!;

        var result1 = weight * 2.5m;
        var result2 = 2.0m * weight;

        Assert.Equal(12.500m, result1.Value);
        Assert.Equal(10.000m, result2.Value);
    }

    [Fact]
    public void Multiplication_WithNegativeScalar_ShouldThrowArgumentException()
    {
        var weight = GoldWeight.Create(5.000m).Value!;

        Assert.Throws<ArgumentException>(() => {
            var _ = weight * -2m;
        });
    }

    [Fact]
    public void Division_GoldWeightByScalar_ShouldReturnCorrectWeight()
    {
        var weight = GoldWeight.Create(10.000m).Value!;
        var result = weight / 2m;

        Assert.Equal(5.000m, result.Value);
    }

    [Fact]
    public void Division_GoldWeightByZeroScalar_ShouldThrowDivideByZeroException()
    {
        var weight = GoldWeight.Create(10.000m).Value!;

        Assert.Throws<DivideByZeroException>(() => {
            var _ = weight / 0m;
        });
    }

    [Fact]
    public void Division_GoldWeightByNegativeScalar_ShouldThrowArgumentException()
    {
        var weight = GoldWeight.Create(10.000m).Value!;

        Assert.Throws<ArgumentException>(() => {
            var _ = weight / -2m;
        });
    }

    [Fact]
    public void Division_GoldWeightByGoldWeight_ShouldReturnCorrectRatio()
    {
        var w1 = GoldWeight.Create(15.000m).Value!;
        var w2 = GoldWeight.Create(5.000m).Value!;

        decimal ratio = w1 / w2;

        Assert.Equal(3.0m, ratio);
    }

    [Fact]
    public void Division_GoldWeightByZeroGoldWeight_ShouldThrowDivideByZeroException()
    {
        var w1 = GoldWeight.Create(15.000m).Value!;

        Assert.Throws<DivideByZeroException>(() => {
            decimal _ = w1 / GoldWeight.Zero;
        });
    }

    [Fact]
    public void Zero_StaticField_ShouldHaveZeroValue()
    {
        Assert.Equal(0.000m, GoldWeight.Zero.Value);
    }

    [Fact]
    public void ToString_ShouldReturnFormattedString()
    {
        var weight = GoldWeight.Create(12.4567m).Value!;

        string formatted = weight.ToString();

        Assert.Equal("12.457 g", formatted);
    }
}
