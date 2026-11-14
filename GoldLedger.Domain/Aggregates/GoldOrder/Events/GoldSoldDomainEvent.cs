using GoldLedger.Domain.Common;
using GoldLedger.Domain.ValueObjects;

public record GoldSoldDomainEvent(Guid OrderId, Guid UserId, GoldWeight Weight, Money TotalAmount) : IDomainEvent;
