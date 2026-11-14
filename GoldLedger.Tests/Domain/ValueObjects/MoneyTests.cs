using GoldLedger.Domain.ValueObjects;
using GoldLedger.Domain.Common;

namespace GoldLedger.Tests.Domain.ValueObjects;

public class MoneyTests
{
    [Fact]
    public void Create_WithValidAmountAndCurrency_ShouldReturnSuccess()
    {
        var result = Money.Create(1500000m, "IRR");

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(1500000m, result.Value.Amount);
        Assert.Equal("IRR", result.Value);
        Assert.Equal(ErrorType.None, result.ErrorType);
    }

    [Fact]
    public void Create_WithNegativeAmount_ShouldReturnFailureWithValidationError()
    {
        var result = Money.Create(-500m, "IRR");

        Assert.True(!result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("     ")]
    public void Create_WithInvalidCurrency_ShouldReturnFailure(string? invalidCurrency)
    {
        var result = Money.Create(1000m, invalidCurrency!);

        Assert.True(!result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public void Addition_WithSameCurrency_ShouldReturnCorrectSum()
    {
        var m1 = Money.Create(1000m, "IRR").Value!;
        var m2 = Money.Create(500m, "IRR").Value!;

        var sum = m1 + m2;

        Assert.NotNull(sum);
        Assert.Equal(1500m, sum.Amount);
        Assert.Equal("IRR", sum);
    }

    [Fact]
    public void Equals_WithSameAmountAndCurrency_ShouldBeEqual()
    {
        var m1 = Money.Create(2000m, "IRT").Value!;
        var m2 = Money.Create(2000m, "IRT").Value!;

        Assert.True(m1 == m2);
        Assert.Equal(m1.GetHashCode(), m2.GetHashCode());
    }

    [Fact]
    public void Equals_WithDifferentCurrencies_ShouldNotBeEqual()
    {
        var m1 = Money.Create(1000m, "IRR").Value!;
        var m2 = Money.Create(1000m, "IRT").Value!;

        Assert.True(m1 != m2);
        Assert.False(m1.Equals(m2));
    }

    [Fact]
    public void Create_WithZeroAmount_ShouldReturnSuccess()
    {
        var result = Money.Create(0m, "IRR");

        Assert.True(result.IsSuccess);
        Assert.Equal(0m, result.Value!.Amount);
    }

    [Fact]
    public void Create_WithLowercaseCurrency_ShouldNormalizeToUpperCase()
    {
        var result = Money.Create(1000m, "irr");

        Assert.True(result.IsSuccess);
        Assert.Equal("IRR", result.Value!);
    }

    [Fact]
    public void Subtraction_WithSameCurrency_ShouldReturnCorrectDifference()
    {
        var m1 = Money.Create(2000m, "IRR").Value!;
        var m2 = Money.Create(500m, "IRR").Value!;

        var diff = m1 - m2;

        Assert.NotNull(diff);
        Assert.Equal(1500m, diff.Amount);
        Assert.Equal("IRR", diff);
    }

    [Fact]
    public void Addition_WithDifferentCurrencies_ShouldThrowInvalidOperationException()
    {
        var m1 = Money.Create(1000m, "IRR").Value!;
        var m2 = Money.Create(1000m, "IRT").Value!;

        Assert.Throws<InvalidOperationException>(() => {
            var result = m1 + m2;
        });
    }

    [Fact]
    public void Subtraction_WithDifferentCurrencies_ShouldThrowInvalidOperationException()
    {
        var m1 = Money.Create(2000m, "IRR").Value!;
        var m2 = Money.Create(500m, "IRT").Value!;

        Assert.Throws<InvalidOperationException>(() => {
            var result = m1 - m2;
        });
    }

    [Fact]
    public void ToString_ShouldReturnFormattedString()
    {
        var money = Money.Create(1500000m, "IRR").Value!;

        var result = money.ToString();

        Assert.Equal("1,500,000 IRR", result);
    }

    [Fact]
    public void ComparisonOperators_WithSameCurrency_ShouldCompareAmountsCorrectly()
    {
        var m1 = Money.Create(1000m, "IRR").Value!;
        var m2 = Money.Create(2000m, "IRR").Value!;

        Assert.True(m1 < m2);
        Assert.True(m2 > m1);
        Assert.True(m1 <= m2);
        Assert.True(m2 >= m1);
    }

    [Fact]
    public void ComparisonOperators_WithDifferentCurrencies_ShouldThrowInvalidOperationException()
    {
        var m1 = Money.Create(1000m, "IRR").Value!;
        var m2 = Money.Create(2000m, "IRT").Value!;

        Assert.Throws<InvalidOperationException>(() => {
            var isGreater = m1 > m2;
        });
    }

    [Fact]
    public void Equals_WithNullOrDifferentType_ShouldReturnFalse()
    {
        var m1 = Money.Create(1000m, "IRR").Value!;

        Assert.False(m1.Equals(null));
    }

    [Fact]
    public void Division_ByValidNumber_ShouldReturnCorrectQuotient()
    {
        var money = Money.Create(3000m, "IRR").Value!;
        var result = money / 3m;

        Assert.Equal(1000m, result.Amount);
    }

    [Fact]
    public void Multiplication_WithValidMultiplier_ShouldReturnCorrectProduct()
    {
        var money = Money.Create(1000m, "IRR").Value!;

        var result = money * 3.5m;

        Assert.NotNull(result);
        Assert.Equal(3500m, result.Amount);
        Assert.Equal("IRR", result);
    }

    [Fact]
    public void Multiplication_WithNegativeMultiplier_ShouldThrowArgumentException()
    {
        var money = Money.Create(1000m, "IRR").Value!;

        Assert.Throws<ArgumentException>(() => {
            var result = money * -2m;
        });
    }

    [Fact]
    public void Create_WithExcessiveDecimalsForIRR_ShouldReturnFailure()
    {
        var result = Money.Create(1000.50m, "IRR");

        Assert.True(!result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public void Multiplication_WithMultiplierOnLeft_ShouldReturnCorrectProduct()
    {
        var money = Money.Create(1000m, "IRR").Value!;

        var result = 2.5m * money;

        Assert.NotNull(result);
        Assert.Equal(2500m, result.Amount);
        Assert.Equal("IRR", result);
    }

    [Fact]
    public void Division_MoneyByMoney_ShouldReturnCorrectRatio()
    {
        var m1 = Money.Create(3000m, "IRR").Value!;
        var m2 = Money.Create(1000m, "IRR").Value!;

        decimal ratio = m1 / m2;

        Assert.Equal(3m, ratio);
    }

    [Fact]
    public void Division_MoneyByZeroDivisor_ShouldThrowDivideByZeroException()
    {
        var money = Money.Create(1000m, "IRR").Value!;

        Assert.Throws<DivideByZeroException>(() => {
            var result = money / 0m;
        });
    }

    [Fact]
    public void Division_MoneyByZeroMoneyAmount_ShouldThrowDivideByZeroException()
    {
        var m1 = Money.Create(1000m, "IRR").Value!;
        var m2 = Money.Create(0m, "IRR").Value!;

        Assert.Throws<DivideByZeroException>(() => {
            decimal ratio = m1 / m2;
        });
    }

    [Fact]
    public void Division_MoneyByMoney_WithDifferentCurrencies_ShouldThrowInvalidOperationException()
    {
        var m1 = Money.Create(3000m, "IRR").Value!;
        var m2 = Money.Create(1000m, "USD").Value!;

        Assert.Throws<InvalidOperationException>(() => {
            decimal ratio = m1 / m2;
        });
    }

    [Theory]
    [InlineData(10.55, true)]
    [InlineData(10.555, false)]
    public void Create_WithVariousDecimalsForUSD_ShouldValidateCorrectly(decimal amount, bool expectedSuccess)
    {
        var result = Money.Create(amount, "USD");

        Assert.Equal(expectedSuccess, result.IsSuccess);
        if (!expectedSuccess)
        {
            Assert.Equal(ErrorType.Validation, result.ErrorType);
        }
    }

    [Fact]
    public void ToString_WithDecimalCurrency_ShouldFormatCorrectly()
    {
        var money = Money.Create(1234.5m, "USD").Value!;

        var result = money.ToString();

        Assert.Equal("1,234.50 USD", result);
    }
}
