using GoldLedger.Domain.Common;
using GoldLedger.Domain.ValueObjects;

namespace GoldLedger.Domain.Aggregates.GoldOrder;

public sealed record GoldOrderSettledDomainEvent(
    Guid OrderId,
    Guid UserId,
    OrderType OrderType,
    Money TotalAmount,
    DateTime OccurredAt) : IDomainEvent;
