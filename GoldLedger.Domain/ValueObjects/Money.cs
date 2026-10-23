using GoldLedger.Domain.Common;

namespace GoldLedger.Domain.ValueObjects;

public class Money : ValueObject<Money>
{
    public decimal Amount { get; }
    public string Currency { get; }

    private Money(decimal amount, string currency)
    {
        Amount = Math.Round(amount, CurrencyRules.GetDecimalPlaces(currency.ToUpperInvariant()));
        Currency = currency.ToUpperInvariant();
    }

    public static Result<Money> Create(decimal amount, string currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
            return Result<Money>.Failure("واحد پول نمی‌تواند خالی باشد.", ErrorType.Validation);

        if (amount < 0)
            return Result<Money>.Failure("مبلغ پولی نمی‌تواند منفی باشد.", ErrorType.Validation);

        string normalizedCurrency = currency.ToUpperInvariant();
        int allowedDecimals = CurrencyRules.GetDecimalPlaces(normalizedCurrency);

        if (HasExcessiveDecimals(amount, allowedDecimals))
        {
            return Result<Money>.Failure(
                allowedDecimals == 0
                    ? $"ارز {normalizedCurrency} نمی‌تواند دارای ارقام اعشار باشد."
                    : $"مبلغ نمی‌تواند بیش از {allowedDecimals} رقم اعشار برای ارز {normalizedCurrency} داشته باشد.",
                ErrorType.Validation);
        }

        return Result<Money>.Success(new Money(amount, normalizedCurrency));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }

    public static Money operator +(Money left, Money right)
    {
        if (left.Currency != right.Currency)
            throw new InvalidOperationException($"امکان جمع دو مبلغ با واحدهای پولی متفاوت ({left.Currency} و {right.Currency}) وجود ندارد.");

        return new Money(left.Amount + right.Amount, left.Currency);
    }

    public static Money operator -(Money left, Money right)
    {
        if (left.Currency != right.Currency)
            throw new InvalidOperationException($"امکان تفریق دو مبلغ با واحدهای پولی متفاوت ({left.Currency} و {right.Currency}) وجود ندارد.");

        return new Money(left.Amount - right.Amount, left.Currency);
    }

    public override string ToString()
    {
        int decimalPlaces = CurrencyRules.GetDecimalPlaces(Currency);
        string format = $"N{decimalPlaces}";
        return $"{Amount.ToString(format)} {Currency}";
    }
    public static bool operator >(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return left.Amount > right.Amount;
    }

    public static bool operator <(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return left.Amount < right.Amount;
    }

    public static bool operator >=(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return left.Amount >= right.Amount;
    }

    public static bool operator <=(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return left.Amount <= right.Amount;
    }

    public static Money operator *(Money money, decimal multiplier)
    {
        if (multiplier < 0)
            throw new ArgumentException("ضریب ضرب نمی‌تواند منفی باشد.", nameof(multiplier));

        return new Money(money.Amount * multiplier, money.Currency);
    }

    private Money CalculatePercentage(decimal percent)
    {
        decimal calculatedAmount = (Amount * percent) / 100m;
        return new Money(calculatedAmount, Currency);
    }

    public static decimal operator /(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        if (right.Amount == 0)
            throw new DivideByZeroException("تقسیم بر مبلغ صفر امکان‌پذیر نیست.");

        return left.Amount / right.Amount;
    }

    public static Money operator *(decimal multiplier, Money money) => money * multiplier;

    public static Money operator /(Money money, decimal divisor)
    {
        if (divisor == 0)
            throw new DivideByZeroException("مخرج تقسیم نمی‌تواند صفر باشد.");

        return new Money(money.Amount / divisor, money.Currency);
    }

    private static void EnsureSameCurrency(Money left, Money right)
    {
        if (left.Currency != right.Currency)
            throw new InvalidOperationException($"امکان مقایسه دو مبلغ با واحدهای پولی متفاوت ({left.Currency} و {right.Currency}) وجود ندارد.");
    }

    private static bool HasExcessiveDecimals(decimal amount, int maxDecimals)
    {
        decimal multiplier = 1m;
        for (int i = 0; i < maxDecimals; i++)
        {
            multiplier *= 10m;
        }

        return (amount * multiplier) % 1m != 0m;
    }

    public Money CalculatePercentage(Percentage percentage)
    {
        ArgumentNullException.ThrowIfNull(percentage);
        return CalculatePercentage(percentage.Value);
    }
}