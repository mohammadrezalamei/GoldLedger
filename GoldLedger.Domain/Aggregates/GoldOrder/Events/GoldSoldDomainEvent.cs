using GoldLedger.Domain.Common;
using GoldLedger.Domain.ValueObjects;

namespace GoldLedger.Domain.Aggregates.GoldOrder;

public record GoldSoldDomainEvent(Guid OrderId, Guid UserId, GoldWeight Weight, Money TotalAmount) : IDomainEvent;


