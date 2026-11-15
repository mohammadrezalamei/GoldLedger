using GoldLedger.Domain.Common;

namespace GoldLedger.Domain.ValueObjects;

public class GoldWeight : ValueObject<GoldWeight>
{
    public decimal Value { get; }

    private GoldWeight(decimal value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                "وزن طلا نمی‌تواند منفی باشد.");
        }

        Value = Math.Round(value, 3, MidpointRounding.AwayFromZero);
    }

    public static Result<GoldWeight> Create(decimal value)
    {
        if (value < 0)
        {
            return Result<GoldWeight>.Failure(
                "وزن طلا نمی‌تواند منفی باشد.",
                ErrorType.Validation);
        }

        return Result<GoldWeight>.Success(new GoldWeight(value));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public static GoldWeight operator +(GoldWeight left, GoldWeight right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        return new GoldWeight(left.Value + right.Value);
    }

    public static GoldWeight operator -(GoldWeight left, GoldWeight right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        if (left.Value < right.Value)
        {
            throw new InvalidOperationException(
                "نتیجه تفریق وزن نمی‌تواند منفی باشد.");
        }

        return new GoldWeight(left.Value - right.Value);
    }

    public static bool operator <(GoldWeight left, GoldWeight right)
        => left.Value < right.Value;

    public static bool operator >(GoldWeight left, GoldWeight right)
        => left.Value > right.Value;

    public static bool operator <=(GoldWeight left, GoldWeight right)
        => left.Value <= right.Value;

    public static bool operator >=(GoldWeight left, GoldWeight right)
        => left.Value >= right.Value;

    public static Money operator *(GoldWeight weight, Money pricePerGram)
    {
        ArgumentNullException.ThrowIfNull(weight);
        ArgumentNullException.ThrowIfNull(pricePerGram);

        return pricePerGram * weight.Value;
    }

    public static Money operator *(Money pricePerGram, GoldWeight weight)
        => weight * pricePerGram;

    public static GoldWeight operator *(GoldWeight weight, decimal multiplier)
    {
        ArgumentNullException.ThrowIfNull(weight);

        if (multiplier < 0)
        {
            throw new ArgumentException(
                "ضریب وزن نمی‌تواند منفی باشد.",
                nameof(multiplier));
        }

        return new GoldWeight(weight.Value * multiplier);
    }

    public static GoldWeight operator *(decimal multiplier, GoldWeight weight)
        => weight * multiplier;

    public static GoldWeight operator /(GoldWeight weight, decimal divisor)
    {
        ArgumentNullException.ThrowIfNull(weight);

        if (divisor == 0)
        {
            throw new DivideByZeroException(
                "تقسیم وزن بر عدد صفر امکان‌پذیر نیست.");
        }

        if (divisor < 0)
        {
            throw new ArgumentException(
                "مخرج تقسیم نمی‌تواند منفی باشد.",
                nameof(divisor));
        }

        return new GoldWeight(weight.Value / divisor);
    }

    public static decimal operator /(GoldWeight left, GoldWeight right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        if (right.Value == 0)
        {
            throw new DivideByZeroException(
                "تقسیم بر وزن صفر امکان‌پذیر نیست.");
        }

        return left.Value / right.Value;
    }

    public static readonly GoldWeight Zero = new(0m);

    public override string ToString() => $"{Value:N3} g";
}
