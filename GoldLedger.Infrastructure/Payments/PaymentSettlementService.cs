using GoldLedger.Application.Contracts.Services;
using GoldLedger.Domain.Common;
namespace GoldLedger.Infrastructure.Payments;
public sealed class PaymentSettlementService : IPaymentSettlementService
{
    public Task<Result> SettleAsync(Guid orderId, Guid userId, decimal amount, CancellationToken cancellationToken = default)
        => Task.FromResult(Result.Success());
}
