using GoldLedger.Application.Contracts.Services;
using GoldLedger.Domain.Aggregates.Payments;
using GoldLedger.Domain.Common;
using GoldLedger.Domain.ValueObjects;
using GoldLedger.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GoldLedger.Infrastructure.Payments;

public sealed class PaymentSettlementService(GoldLedgerDbContext context) : IPaymentSettlementService
{
    public async Task<Result> SettleAsync(
        Guid orderId,
        Guid userId,
        OrderType orderType,
        Money amount,
        DateTime occurredAt,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var settlementResult = PaymentSettlement.Create(orderId, userId, orderType, amount, occurredAt);
        if (!settlementResult.IsSuccess)
            return Result.Failure(settlementResult.Message!, settlementResult.ErrorType);

        var settlement = settlementResult.Value!;
        var existingSettlement = context.PaymentSettlements.Local.SingleOrDefault(entry => entry.OrderId == orderId)
            ?? await context.PaymentSettlements.AsNoTracking()
                .SingleOrDefaultAsync(entry => entry.OrderId == orderId, cancellationToken);

        if (existingSettlement is not null)
        {
            if (!existingSettlement.Matches(settlement))
                return Result.Failure("تسویه قبلی سفارش با درخواست جدید مطابقت ندارد.", ErrorType.Conflict);

            return Result.Success();
        }

        await context.PaymentSettlements.AddAsync(settlement, cancellationToken);
        return Result.Success();
    }
}
