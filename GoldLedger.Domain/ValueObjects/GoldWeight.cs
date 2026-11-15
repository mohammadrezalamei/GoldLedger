using GoldLedger.Domain.Common;

namespace GoldLedger.Domain.ValueObjects;

public class GoldWeight : ValueObject<GoldWeight>
{
    public decimal Value { get; }

    private GoldWeight(decimal value)
    {
        Value = value;
    }

    public static Result<GoldWeight> Create(decimal value)
    {
        var normalizedValue = Math.Round(value, 3, MidpointRounding.AwayFromZero);

        if (normalizedValue < 0)
            return Result<GoldWeight>.Failure("ÙˆØ²Ù† Ø·Ù„Ø§ Ù†Ù…ÛŒâ€ŒØªÙˆØ§Ù†Ø¯ Ù…Ù†ÙÛŒ Ø¨Ø§Ø´Ø¯.", ErrorType.Validation);

        return Result<GoldWeight>.Success(new GoldWeight(normalizedValue));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public static GoldWeight operator +(GoldWeight left, GoldWeight right) => new(left.Value + right.Value);
    public static GoldWeight operator -(GoldWeight left, GoldWeight right) => new(left.Value - right.Value);
    public static bool operator <(GoldWeight left, GoldWeight right) => left.Value < right.Value;
    public static bool operator >(GoldWeight left, GoldWeight right) => left.Value > right.Value;
    public static bool operator <=(GoldWeight left, GoldWeight right) => left.Value <= right.Value;
    public static bool operator >=(GoldWeight left, GoldWeight right) => left.Value >= right.Value;
    public static Money operator *(GoldWeight weight, Money pricePerGram) => pricePerGram * weight.Value;
    public static Money operator *(Money pricePerGram, GoldWeight weight) => pricePerGram * weight.Value;
    public static GoldWeight operator *(GoldWeight weight, decimal multiplier)
    {
        if (multiplier < 0)
            throw new ArgumentException("Ø¶Ø±ÛŒØ¨ ÙˆØ²Ù† Ù†Ù…ÛŒâ€ŒØªÙˆØ§Ù†Ø¯ Ù…Ù†ÙÛŒ Ø¨Ø§Ø´Ø¯.");
        return new(Math.Round(weight.Value * multiplier, 3, MidpointRounding.AwayFromZero));
    }

    public static GoldWeight operator *(decimal multiplier, GoldWeight weight) => weight * multiplier;

    public static GoldWeight operator /(GoldWeight weight, decimal divisor)
    {
        if (divisor == 0)
            throw new DivideByZeroException("ØªÙ‚Ø³ÛŒÙ… ÙˆØ²Ù† Ø¨Ø± Ø¹Ø¯Ø¯ ØµÙØ± Ø§Ù…Ú©Ø§Ù†â€ŒÙ¾Ø°ÛŒØ± Ù†ÛŒØ³Øª.");
        if (divisor < 0)
            throw new ArgumentException("Ù…Ø®Ø±Ø¬ ØªÙ‚Ø³ÛŒÙ… Ù†Ù…ÛŒâ€ŒØªÙˆØ§Ù†Ø¯ Ù…Ù†ÙÛŒ Ø¨Ø§Ø´Ø¯.");

        return new(Math.Round(weight.Value / divisor, 3, MidpointRounding.AwayFromZero));
    }

    public static decimal operator /(GoldWeight left, GoldWeight right)
    {
        if (right.Value == 0)
            throw new DivideByZeroException("ØªÙ‚Ø³ÛŒÙ… Ø¨Ø± ÙˆØ²Ù† ØµÙØ± Ø§Ù…Ú©Ø§Ù†â€ŒÙ¾Ø°ÛŒØ± Ù†ÛŒØ³Øª.");

        return left.Value / right.Value;
    }
    public static readonly GoldWeight Zero = new(0m);

    public override string ToString() => $"{Value:N3} g";
}

