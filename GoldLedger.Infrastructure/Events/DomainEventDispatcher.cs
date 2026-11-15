using System.Text.Json;
using GoldLedger.Application.Contracts.Common;
using GoldLedger.Application.Contracts.Services;
using GoldLedger.Domain.Aggregates.GoldOrder;
using GoldLedger.Domain.Common;
using GoldLedger.Infrastructure.Persistence;

namespace GoldLedger.Infrastructure.Events;

public sealed class DomainEventDispatcher(
    GoldLedgerDbContext context,
    IPaymentSettlementService settlementService) : IDomainEventDispatcher
{
    public async Task<Result> DispatchAsync(
        IReadOnlyCollection<IDomainEvent> domainEvents,
        CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in domainEvents)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Guid orderId;
            switch (domainEvent)
            {
                case GoldOrderSettledDomainEvent settled:
                    var settlementResult = await settlementService.SettleAsync(
                        settled.OrderId,
                        settled.UserId,
                        settled.OrderType,
                        settled.TotalAmount,
                        settled.OccurredAt,
                        cancellationToken);

                    if (!settlementResult.IsSuccess)
                        return settlementResult;

                    orderId = settled.OrderId;
                    break;
                case GoldPurchasedDomainEvent purchased:
                    orderId = purchased.OrderId;
                    break;
                case GoldSoldDomainEvent sold:
                    orderId = sold.OrderId;
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported domain event: {domainEvent.GetType().Name}");
            }

            context.DomainEventRecords.Add(new DomainEventRecord(
                orderId,
                domainEvent.GetType().Name,
                JsonSerializer.Serialize(domainEvent, domainEvent.GetType())));
        }

        return Result.Success();
    }
}
