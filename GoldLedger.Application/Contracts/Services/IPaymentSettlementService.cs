using GoldLedger.Domain.Common;
namespace GoldLedger.Application.Contracts.Services;
public interface IPaymentSettlementService
{
    Task<Result> SettleAsync(Guid orderId, Guid userId, decimal amount, CancellationToken cancellationToken = default);
}

