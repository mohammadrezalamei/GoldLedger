using GoldLedger.Domain.Common;

namespace GoldLedger.Domain.ValueObjects;

public class Percentage : ValueObject<Percentage>
{
    public decimal Value { get; }

    private Percentage(decimal value)
    {
        if (value < 0 || value > 100)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                "درصد کارمزد باید عددی بین ۰ تا ۱۰۰ باشد.");
        }

        Value = Math.Round(value, 2, MidpointRounding.AwayFromZero);
    }

    public static Result<Percentage> Create(decimal value)
    {
        if (value < 0 || value > 100)
        {
            return Result<Percentage>.Failure(
                "درصد کارمزد باید عددی بین ۰ تا ۱۰۰ باشد.",
                ErrorType.Validation);
        }

        return Result<Percentage>.Success(new Percentage(value));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public decimal CalculateAmount(decimal baseAmount)
    {
        return (baseAmount * Value) / 100m;
    }

    public Money CalculateMoney(Money money)
    {
        ArgumentNullException.ThrowIfNull(money);

        return money.CalculatePercentage(this);
    }

    public static Percentage operator +(Percentage left, Percentage right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        var sum = left.Value + right.Value;

        if (sum > 100)
        {
            throw new InvalidOperationException(
                "مجموع درصدها نمی‌تواند بیشتر از ۱۰۰ باشد.");
        }

        return new Percentage(sum);
    }

    public static Percentage operator -(Percentage left, Percentage right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        if (left.Value < right.Value)
        {
            throw new InvalidOperationException(
                "نتیجه تفریق درصد نمی‌تواند منفی باشد.");
        }

        return new Percentage(left.Value - right.Value);
    }

    public static bool operator <(Percentage left, Percentage right)
        => left.Value < right.Value;

    public static bool operator >(Percentage left, Percentage right)
        => left.Value > right.Value;

    public static bool operator <=(Percentage left, Percentage right)
        => left.Value <= right.Value;

    public static bool operator >=(Percentage left, Percentage right)
        => left.Value >= right.Value;

    public static readonly Percentage Zero = new(0m);

    public override string ToString() => $"{Value:N2}%";
}
