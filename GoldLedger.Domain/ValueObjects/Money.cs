using GoldLedger.Domain.Common;

namespace GoldLedger.Domain.ValueObjects;

public sealed class Money : ValueObject<Money>
{
    public decimal Amount { get; }

    private Money(decimal amount)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(amount),
                "مبلغ نمی‌تواند منفی باشد.");
        }

        Amount = decimal.Round(
            amount,
            2,
            MidpointRounding.ToEven);
    }

    public static Result<Money> Create(decimal amount)
    {
        if (amount < 0)
        {
            return Result<Money>.Failure(
                "مبلغ نمی‌تواند منفی باشد.",
                ErrorType.Validation);
        }

        return Result<Money>.Success(new Money(amount));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
    }

    public static Money operator +(Money left, Money right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        return new Money(left.Amount + right.Amount);
    }

    public static Money operator -(Money left, Money right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        if (left.Amount < right.Amount)
        {
            throw new InvalidOperationException(
                "نتیجه تفریق مبلغ نمی‌تواند منفی باشد.");
        }

        return new Money(left.Amount - right.Amount);
    }

    public static bool operator >(Money left, Money right)
        => left.Amount > right.Amount;

    public static bool operator <(Money left, Money right)
        => left.Amount < right.Amount;

    public static bool operator >=(Money left, Money right)
        => left.Amount >= right.Amount;

    public static bool operator <=(Money left, Money right)
        => left.Amount <= right.Amount;

    public static Money operator *(Money money, decimal multiplier)
    {
        ArgumentNullException.ThrowIfNull(money);

        if (multiplier < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(multiplier),
                "ضریب مبلغ نمی‌تواند منفی باشد.");
        }

        return new Money(money.Amount * multiplier);
    }

    public static Money operator *(decimal multiplier, Money money)
        => money * multiplier;

    public static Money operator /(Money money, decimal divisor)
    {
        ArgumentNullException.ThrowIfNull(money);

        if (divisor == 0)
        {
            throw new DivideByZeroException(
                "تقسیم مبلغ بر عدد صفر امکان‌پذیر نیست.");
        }

        if (divisor < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(divisor),
                "مخرج تقسیم نمی‌تواند منفی باشد.");
        }

        return new Money(money.Amount / divisor);
    }

    public static decimal operator /(Money left, Money right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        if (right.Amount == 0)
        {
            throw new DivideByZeroException(
                "تقسیم بر مبلغ صفر امکان‌پذیر نیست.");
        }

        return left.Amount / right.Amount;
    }

    public Money CalculatePercentage(Percentage percentage)
    {
        ArgumentNullException.ThrowIfNull(percentage);

        return new Money((Amount * percentage.Value) / 100m);
    }

    public override string ToString() => Amount.ToString("N2");
}
