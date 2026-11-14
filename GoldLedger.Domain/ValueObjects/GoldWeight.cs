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
            return Result<GoldWeight>.Failure("وزن طلا نمی‌تواند منفی باشد.", ErrorType.Validation);

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
            throw new ArgumentException("ضریب وزن نمی‌تواند منفی باشد.");
        return new(Math.Round(weight.Value * multiplier, 3, MidpointRounding.AwayFromZero));
    }

    public static GoldWeight operator *(decimal multiplier, GoldWeight weight) => weight * multiplier;

    public static GoldWeight operator /(GoldWeight weight, decimal divisor)
    {
        if (divisor == 0)
            throw new DivideByZeroException("تقسیم وزن بر عدد صفر امکان‌پذیر نیست.");
        if (divisor < 0)
            throw new ArgumentException("مخرج تقسیم نمی‌تواند منفی باشد.");

        return new(Math.Round(weight.Value / divisor, 3, MidpointRounding.AwayFromZero));
    }

    public static decimal operator /(GoldWeight left, GoldWeight right)
    {
        if (right.Value == 0)
            throw new DivideByZeroException("تقسیم بر وزن صفر امکان‌پذیر نیست.");

        return left.Value / right.Value;
    }
    public static readonly GoldWeight Zero = new(0m);

    public override string ToString() => $"{Value:N3} g";
}
