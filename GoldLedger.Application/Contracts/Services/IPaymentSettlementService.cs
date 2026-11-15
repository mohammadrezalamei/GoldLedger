using GoldLedger.Domain.Common;
using GoldLedger.Domain.ValueObjects;

namespace GoldLedger.Application.Contracts.Services;

public interface IPaymentSettlementService
{
    Task<Result> SettleAsync(
        Guid orderId,
        Guid userId,
        OrderType orderType,
        Money amount,
        DateTime occurredAt,
        CancellationToken cancellationToken = default);
}
