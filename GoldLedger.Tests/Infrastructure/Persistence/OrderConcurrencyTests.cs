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

public sealed class OrderConcurrencyTests
{
    [Fact]
    public async Task Stale_wallet_rolls_back_order_settlement_and_completion_event_then_fresh_retry_succeeds()
    {
        await using var database = new SqliteTestDatabase();
        await database.InitializeAsync();
        var userId = await database.SeedWalletAsync();
        Guid firstOrderId;
        Guid secondOrderId;
        await using (var setup = database.CreateContext())
        {
            var service = SqliteTestDatabase.CreateOrderService(setup);
            firstOrderId = (await service.CreateOrderAsync(Request(userId))).Value;
            secondOrderId = (await service.CreateOrderAsync(Request(userId))).Value;
        }
        await using var firstContext = database.CreateContext();
        await using var staleContext = database.CreateContext();
        await firstContext.Wallets.SingleAsync();
        await staleContext.Wallets.SingleAsync();

        Assert.True((await SqliteTestDatabase.CreateOrderService(firstContext).CompleteOrderAsync(firstOrderId)).IsSuccess);
        var rejected = await SqliteTestDatabase.CreateOrderService(staleContext).CompleteOrderAsync(secondOrderId);

        Assert.Equal(ErrorType.Conflict, rejected.ErrorType);
        Assert.Empty(staleContext.ChangeTracker.Entries());
        await using var verification = database.CreateContext();
        Assert.Equal(OrderStatus.Pending, (await verification.Orders.SingleAsync(order => order.Id == secondOrderId)).Status);
        Assert.Single(await verification.PaymentSettlements.ToListAsync());
        Assert.Equal(3, await verification.DomainEventRecords.CountAsync());
        var wallet = await verification.Wallets.SingleAsync();
        Assert.Equal(4m, wallet.Balance.Value);
        Assert.Equal(1m, wallet.LockedBalance.Value);

        Assert.True((await SqliteTestDatabase.CreateOrderService(staleContext).CompleteOrderAsync(secondOrderId)).IsSuccess);
        Assert.Equal(2, await verification.PaymentSettlements.CountAsync());
    }

    [Fact]
    public async Task Tag_only_updates_use_parent_concurrency_token_and_preserve_winner()
    {
        await using var database = new SqliteTestDatabase();
        await database.InitializeAsync();
        var userId = await database.SeedWalletAsync();
        await using var firstContext = database.CreateContext();
        var created = await SqliteTestDatabase.CreateOrderService(firstContext).CreateOrderAsync(Request(userId));
        var firstOrder = await firstContext.Orders.Include(order => order.Tags).SingleAsync();
        await using var staleContext = database.CreateContext();
        var staleOrder = await staleContext.Orders.Include(order => order.Tags).SingleAsync();
        firstOrder.UpdateMetadata([Tag.Create("winner").Value!], firstOrder.UserNote);
        staleOrder.UpdateMetadata([Tag.Create("loser").Value!], staleOrder.UserNote);

        Assert.True((await SqliteTestDatabase.CreateUnitOfWork(firstContext).SaveChangesAsync()).IsSuccess);
        Assert.Equal(ErrorType.Conflict, (await SqliteTestDatabase.CreateUnitOfWork(staleContext).SaveChangesAsync()).ErrorType);

        await using var verification = database.CreateContext();
        var persisted = await verification.Orders.Include(order => order.Tags).SingleAsync();
        Assert.Equal("winner", Assert.Single(persisted.Tags).Value);
        Assert.Empty(staleContext.ChangeTracker.Entries());
        Assert.Equal(created.Value, persisted.Id);
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, false)]
    public async Task Concurrent_order_transitions_have_one_winner_and_consistent_settlement(
        bool firstCompletes, bool secondCompletes)
    {
        await using var database = new SqliteTestDatabase();
        await database.InitializeAsync();
        var userId = await database.SeedWalletAsync();
        Guid orderId;
        await using (var setup = database.CreateContext())
            orderId = (await SqliteTestDatabase.CreateOrderService(setup).CreateOrderAsync(Request(userId))).Value;

        using var barrier = new Barrier(2);
        async Task<Result> TransitionAsync(bool complete)
        {
            await using var context = database.CreateContext();
            await context.Orders.SingleAsync();
            await context.Wallets.SingleAsync();
            if (!barrier.SignalAndWait(TimeSpan.FromSeconds(15)))
                throw new TimeoutException("Concurrent transition did not reach the barrier.");
            var service = SqliteTestDatabase.CreateOrderService(context);
            if (complete)
                return await service.CompleteOrderAsync(orderId);
            return await service.CancelOrderAsync(orderId);
        }

        var results = await Task.WhenAll(Task.Run(() => TransitionAsync(firstCompletes)),
            Task.Run(() => TransitionAsync(secondCompletes)));

        Assert.Single(results, result => result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, Assert.Single(results, result => !result.IsSuccess).ErrorType);
        await using var verification = database.CreateContext();
        var persistedOrder = await verification.Orders.SingleAsync();
        var wallet = await verification.Wallets.SingleAsync();
        var settled = persistedOrder.Status == OrderStatus.Settled;
        Assert.Equal(settled ? 1 : 0, await verification.PaymentSettlements.CountAsync());
        Assert.Equal(settled ? 4m : 5m, wallet.Balance.Value);
        Assert.Equal(0m, wallet.LockedBalance.Value);
        Assert.Equal(settled ? 2 : 1, await verification.DomainEventRecords.CountAsync());
    }

    private static CreateOrderRequestDto Request(Guid userId, OrderType orderType = OrderType.Sell)
    {
        return new CreateOrderRequestDto(userId, orderType, 1m, 100m, 1m, ["gold"], "settlement test");
    }
}
