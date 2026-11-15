using GoldLedger.Domain.Common;

namespace GoldLedger.Domain.ValueObjects;

public class Percentage : ValueObject<Percentage>
{
    public decimal Value { get; }

    private Percentage(decimal value)
    {
        Value = value;
    }

    public static Result<Percentage> Create(decimal value)
    {
        var normalizedValue = Math.Round(value, 2, MidpointRounding.AwayFromZero);

        if (normalizedValue < 0 || normalizedValue > 100)
            return Result<Percentage>.Failure("Ø¯Ø±ØµØ¯ Ú©Ø§Ø±Ù…Ø²Ø¯ Ø¨Ø§ÛŒØ¯ Ø¹Ø¯Ø¯ÛŒ Ø¨ÛŒÙ† Û° ØªØ§ Û±Û°Û° Ø¨Ø§Ø´Ø¯.", ErrorType.Validation);

        return Result<Percentage>.Success(new Percentage(normalizedValue));
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

    public static Percentage operator +(Percentage left, Percentage right) => new(left.Value + right.Value);
    public static Percentage operator -(Percentage left, Percentage right) => new(left.Value - right.Value);

    public static bool operator <(Percentage left, Percentage right) => left.Value < right.Value;
    public static bool operator >(Percentage left, Percentage right) => left.Value > right.Value;
    public static bool operator <=(Percentage left, Percentage right) => left.Value <= right.Value;
    public static bool operator >=(Percentage left, Percentage right) => left.Value >= right.Value;

    public static readonly Percentage Zero = new(0m);

    public override string ToString() => $"{Value:N2}%";
}

