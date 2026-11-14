using GoldLedger.Domain.Common;
namespace GoldLedger.Domain.ValueObjects;
public sealed class Money : ValueObject<Money>
{
    public decimal Amount { get; }
    private Money(decimal amount) => Amount = decimal.Round(amount, 2, MidpointRounding.ToEven);
    public static Result<Money> Create(decimal amount) => amount < 0 ? Result<Money>.Failure("???? ????????? ???? ????.", ErrorType.Validation) : Result<Money>.Success(new Money(amount));
    [Obsolete("Currency is no longer part of the monetary model.")]
    public static Result<Money> Create(decimal amount, string _) => Create(amount);
    protected override IEnumerable<object?> GetEqualityComponents() { yield return Amount; }
    public static Money operator +(Money left, Money right) => new(left.Amount + right.Amount);
    public static Money operator -(Money left, Money right) => new(left.Amount - right.Amount);
    public static bool operator >(Money left, Money right) => left.Amount > right.Amount;
    public static bool operator <(Money left, Money right) => left.Amount < right.Amount;
    public static bool operator >=(Money left, Money right) => left.Amount >= right.Amount;
    public static bool operator <=(Money left, Money right) => left.Amount <= right.Amount;
    public static Money operator *(Money money, decimal multiplier) => new(money.Amount * multiplier);
    public static Money operator *(decimal multiplier, Money money) => money * multiplier;
    public static Money operator /(Money money, decimal divisor) => new(money.Amount / divisor);
    public static decimal operator /(Money left, Money right) => left.Amount / right.Amount;
    public Money CalculatePercentage(Percentage percentage) { ArgumentNullException.ThrowIfNull(percentage); return new Money((Amount * percentage.Value) / 100m); }
    public override string ToString() => Amount.ToString("N2");
}

