using GoldLedger.Domain.Common;
using GoldLedger.Domain.ValueObjects;

namespace GoldLedger.Domain.Aggregates.Payments;

public sealed class PaymentSettlement
{
    public Guid OrderId { get; private set; }
    public Guid UserId { get; private set; }
    public OrderType OrderType { get; private set; }
    public Money Amount { get; private set; } = null!;
    public DateTime SettledAt { get; private set; }

    private PaymentSettlement()
    {
    }

    public static Result<PaymentSettlement> Create(
        Guid orderId,
        Guid userId,
        OrderType orderType,
        Money amount,
        DateTime settledAt)
    {
        if (orderId == Guid.Empty || userId == Guid.Empty)
            return Result<PaymentSettlement>.Failure("شناسه سفارش یا کاربر نامعتبر است.", ErrorType.Validation);

        if (orderType is not (OrderType.Buy or OrderType.Sell))
            return Result<PaymentSettlement>.Failure("نوع تسویه نامعتبر است.", ErrorType.Validation);

        if (amount is null)
            return Result<PaymentSettlement>.Failure("مبلغ تسویه نمی‌تواند خالی باشد.", ErrorType.Validation);

        return Result<PaymentSettlement>.Success(new PaymentSettlement
        {
            OrderId = orderId,
            UserId = userId,
            OrderType = orderType,
            Amount = amount,
            SettledAt = settledAt
        });
    }

    public bool Matches(PaymentSettlement other)
    {
        return OrderId == other.OrderId &&
               UserId == other.UserId &&
               OrderType == other.OrderType &&
               Amount == other.Amount;
    }
}
