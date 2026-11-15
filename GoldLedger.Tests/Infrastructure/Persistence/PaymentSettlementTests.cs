using GoldLedger.Application.DTOs.Orders;
using GoldLedger.Application.Services;
using GoldLedger.Domain.Aggregates.GoldWallet;
using GoldLedger.Domain.Common;
using GoldLedger.Domain.ValueObjects;
using GoldLedger.Infrastructure.Payments;
using GoldLedger.Infrastructure.Persistence;
using GoldLedger.Infrastructure.Repositories;
using GoldLedger.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore;

namespace GoldLedger.Tests.Infrastructure.Persistence;

public sealed class PaymentSettlementTests
{
    [Theory]
    [InlineData(OrderType.Buy, 101, 6)]
    [InlineData(OrderType.Sell, 99, 4)]
    public async Task Completion_commits_wallet_settlement_and_event_together(
        OrderType orderType, decimal expectedAmount, decimal expectedBalance)
    {
        await using var database = new SqliteTestDatabase();
        await database.InitializeAsync();
        var userId = await database.SeedWalletAsync();
        await using var context = database.CreateContext();
        var service = SqliteTestDatabase.CreateOrderService(context);
        var created = await service.CreateOrderAsync(Request(userId, orderType));
        Assert.True(created.IsSuccess);

        Assert.True((await service.CompleteOrderAsync(created.Value)).IsSuccess);
        Assert.False((await service.CompleteOrderAsync(created.Value)).IsSuccess);

        await using var verification = database.CreateContext();
        var settlement = await verification.PaymentSettlements.SingleAsync();
        Assert.Equal(expectedAmount, settlement.Amount.Amount);
        Assert.Equal(orderType, settlement.OrderType);
        Assert.Equal(created.Value, settlement.OrderId);
        Assert.Equal(expectedBalance, (await verification.Wallets.SingleAsync()).Balance.Value);
        Assert.Equal(OrderStatus.Settled, (await verification.Orders.SingleAsync()).Status);
        Assert.Equal(2, await verification.DomainEventRecords.CountAsync());
        Assert.Equal(DateTimeKind.Utc, settlement.SettledAt.Kind);
    }

    [Fact]
    public async Task Repeated_settlement_accepts_identical_details_and_rejects_changed_amount()
    {
        await using var database = new SqliteTestDatabase();
        await database.InitializeAsync();
        var userId = await database.SeedWalletAsync();
        await using var context = database.CreateContext();
        var service = SqliteTestDatabase.CreateOrderService(context);
        var created = await service.CreateOrderAsync(Request(userId));
        await service.CompleteOrderAsync(created.Value);
        var settlement = await context.PaymentSettlements.SingleAsync();
        var payments = new PaymentSettlementService(context);

        Assert.True((await payments.SettleAsync(created.Value, userId, OrderType.Sell,
            Money.Create(99m).Value!, settlement.SettledAt)).IsSuccess);
        Assert.Equal(ErrorType.Conflict, (await payments.SettleAsync(created.Value, userId, OrderType.Sell,
            Money.Create(100m).Value!, settlement.SettledAt)).ErrorType);
        Assert.Single(await context.PaymentSettlements.ToListAsync());
    }

    private static CreateOrderRequestDto Request(Guid userId, OrderType orderType = OrderType.Sell)
    {
        return new CreateOrderRequestDto(userId, orderType, 1m, 100m, 1m, ["gold"], "settlement test");
    }
}
