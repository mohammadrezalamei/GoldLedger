using GoldLedger.Application.DTOs.Orders;
using GoldLedger.Application.Services;
using GoldLedger.Domain.Aggregates.GoldWallet;
using GoldLedger.Domain.Common;
using GoldLedger.Domain.ValueObjects;
using GoldLedger.Infrastructure.Payments;
using GoldLedger.Infrastructure.Persistence;
using GoldLedger.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace GoldLedger.Tests.Infrastructure;

public sealed class TransactionalIntegrityTests
{
    [Fact]
    public async Task Upgrade_from_initial_schema_preserves_wallet_and_matches_current_model()
    {
        await using var database = new LedgerTestDatabase();
        await using var context = database.CreateContext();
        await context.GetService<IMigrator>().MigrateAsync("20261114190531_InitialCreate");
        var userId = Guid.NewGuid();
        context.Wallets.Add(GoldWallet.Create(userId, GoldWeight.Create(7m).Value!).Value!);
        await context.SaveChangesAsync();

        await context.Database.MigrateAsync();
        context.ChangeTracker.Clear();

        Assert.Equal(7m, (await context.Wallets.SingleAsync()).Balance.Value);
        Assert.Empty(await context.Database.GetPendingMigrationsAsync());
        Assert.False(context.Database.HasPendingModelChanges());
        Assert.Empty(await context.PaymentSettlements.ToListAsync());
        Assert.Empty(await context.IdempotencyRecords.ToListAsync());
    }

    [Theory]
    [InlineData(OrderType.Buy, 101, 6)]
    [InlineData(OrderType.Sell, 99, 4)]
    public async Task Completion_commits_wallet_settlement_and_event_together(
        OrderType orderType, decimal expectedAmount, decimal expectedBalance)
    {
        await using var database = new LedgerTestDatabase();
        await database.InitializeAsync();
        var userId = await database.SeedWalletAsync();
        await using var context = database.CreateContext();
        var service = LedgerTestDatabase.CreateOrderService(context);
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
    public async Task Stale_wallet_rolls_back_order_settlement_and_completion_event_then_fresh_retry_succeeds()
    {
        await using var database = new LedgerTestDatabase();
        await database.InitializeAsync();
        var userId = await database.SeedWalletAsync();
        Guid firstOrderId;
        Guid secondOrderId;
        await using (var setup = database.CreateContext())
        {
            var service = LedgerTestDatabase.CreateOrderService(setup);
            firstOrderId = (await service.CreateOrderAsync(Request(userId))).Value;
            secondOrderId = (await service.CreateOrderAsync(Request(userId))).Value;
        }
        await using var firstContext = database.CreateContext();
        await using var staleContext = database.CreateContext();
        await firstContext.Wallets.SingleAsync();
        await staleContext.Wallets.SingleAsync();

        Assert.True((await LedgerTestDatabase.CreateOrderService(firstContext).CompleteOrderAsync(firstOrderId)).IsSuccess);
        var rejected = await LedgerTestDatabase.CreateOrderService(staleContext).CompleteOrderAsync(secondOrderId);

        Assert.Equal(ErrorType.Conflict, rejected.ErrorType);
        Assert.Empty(staleContext.ChangeTracker.Entries());
        await using var verification = database.CreateContext();
        Assert.Equal(OrderStatus.Pending, (await verification.Orders.SingleAsync(order => order.Id == secondOrderId)).Status);
        Assert.Single(await verification.PaymentSettlements.ToListAsync());
        Assert.Equal(3, await verification.DomainEventRecords.CountAsync());
        var wallet = await verification.Wallets.SingleAsync();
        Assert.Equal(4m, wallet.Balance.Value);
        Assert.Equal(1m, wallet.LockedBalance.Value);

        Assert.True((await LedgerTestDatabase.CreateOrderService(staleContext).CompleteOrderAsync(secondOrderId)).IsSuccess);
        Assert.Equal(2, await verification.PaymentSettlements.CountAsync());
    }

    [Fact]
    public async Task Tag_only_updates_use_parent_concurrency_token_and_preserve_winner()
    {
        await using var database = new LedgerTestDatabase();
        await database.InitializeAsync();
        var userId = await database.SeedWalletAsync();
        await using var firstContext = database.CreateContext();
        var created = await LedgerTestDatabase.CreateOrderService(firstContext).CreateOrderAsync(Request(userId));
        var firstOrder = await firstContext.Orders.Include(order => order.Tags).SingleAsync();
        await using var staleContext = database.CreateContext();
        var staleOrder = await staleContext.Orders.Include(order => order.Tags).SingleAsync();
        firstOrder.UpdateMetadata([Tag.Create("winner").Value!], firstOrder.UserNote);
        staleOrder.UpdateMetadata([Tag.Create("loser").Value!], staleOrder.UserNote);

        Assert.True((await LedgerTestDatabase.CreateUnitOfWork(firstContext).SaveChangesAsync()).IsSuccess);
        Assert.Equal(ErrorType.Conflict, (await LedgerTestDatabase.CreateUnitOfWork(staleContext).SaveChangesAsync()).ErrorType);

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
        await using var database = new LedgerTestDatabase();
        await database.InitializeAsync();
        var userId = await database.SeedWalletAsync();
        Guid orderId;
        await using (var setup = database.CreateContext())
            orderId = (await LedgerTestDatabase.CreateOrderService(setup).CreateOrderAsync(Request(userId))).Value;

        using var barrier = new Barrier(2);
        async Task<Result> TransitionAsync(bool complete)
        {
            await using var context = database.CreateContext();
            await context.Orders.SingleAsync();
            await context.Wallets.SingleAsync();
            if (!barrier.SignalAndWait(TimeSpan.FromSeconds(15)))
                throw new TimeoutException("Concurrent transition did not reach the barrier.");
            var service = LedgerTestDatabase.CreateOrderService(context);
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

    [Fact]
    public async Task Repeated_settlement_accepts_identical_details_and_rejects_changed_amount()
    {
        await using var database = new LedgerTestDatabase();
        await database.InitializeAsync();
        var userId = await database.SeedWalletAsync();
        await using var context = database.CreateContext();
        var service = LedgerTestDatabase.CreateOrderService(context);
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

    [Fact]
    public async Task Business_failure_discards_uncommitted_wallet_changes()
    {
        await using var database = new LedgerTestDatabase();
        await database.InitializeAsync();
        var userId = await database.SeedWalletAsync();
        await using var context = database.CreateContext();
        var wallet = await context.Wallets.SingleAsync();
        wallet.Deposit(GoldWeight.Create(1m).Value!);
        var service = new WalletService(new GoldWalletRepository(context), LedgerTestDatabase.CreateUnitOfWork(context));

        var failure = await service.WithdrawAsync(userId, new(decimal.MaxValue));

        Assert.False(failure.IsSuccess);
        Assert.Empty(context.ChangeTracker.Entries());
        await context.SaveChangesAsync();
        await using var verification = database.CreateContext();
        Assert.Equal(5m, (await verification.Wallets.SingleAsync()).Balance.Value);
    }

    private static CreateOrderRequestDto Request(Guid userId, OrderType orderType = OrderType.Sell)
    {
        return new CreateOrderRequestDto(userId, orderType, 1m, 100m, 1m, ["gold"], "settlement test");
    }
}
