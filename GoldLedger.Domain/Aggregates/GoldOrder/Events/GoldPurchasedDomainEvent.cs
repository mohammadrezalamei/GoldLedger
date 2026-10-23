using GoldLedger.Domain.Common;
using GoldLedger.Domain.ValueObjects;

namespace GoldLedger.Domain.Aggregates.GoldOrder;

public record GoldPurchasedDomainEvent(Guid OrderId, Guid UserId, GoldWeight Weight, Money TotalAmount) : IDomainEvent;