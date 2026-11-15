using GoldLedger.Application.DTOs.Orders;
using GoldLedger.Application.Services;
using GoldLedger.Domain.Aggregates.GoldOrder;
using GoldLedger.Domain.Aggregates.GoldWallet;
using GoldLedger.Domain.Common;
using GoldLedger.Domain.ValueObjects;
using GoldLedger.Infrastructure.Persistence;
using GoldLedger.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace GoldLedger.Tests.Application.Services;

public sealed class OrderServiceTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task Failed_sell_transition_does_not_persist_partial_changes(
        bool cancel,
        bool missingWallet)
    {
        await using var connection =
            new SqliteConnection("Data Source=:memory:");

        await connection.OpenAsync();

        var options = CreateOptions(connection);

        await using var context = new GoldLedgerDbContext(options);

        await context.Database.EnsureCreatedAsync();

        var userId = Guid.NewGuid();

        if (!missingWallet)
        {
            var wallet = GoldWallet.Create(
                userId,
                GoldWeight.Create(5m).Value!).Value!;

            context.Wallets.Add(wallet);
        }

        var order = GoldOrder.Create(
            userId,
            OrderType.Sell,
            GoldWeight.Create(1m).Value!,
            Money.Create(100m).Value!,
            Percentage.Create(1m).Value!,
            Array.Empty<Tag>()).Value!;

        context.Orders.Add(order);

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var service = CreateService(context);

        var result = cancel
            ? await service.CancelOrderAsync(order.Id)
            : await service.CompleteOrderAsync(order.Id);

        Assert.False(result.IsSuccess);

        Assert.Equal(
            missingWallet ? ErrorType.NotFound : ErrorType.Validation,
            result.ErrorType);

        Assert.Empty(context.ChangeTracker.Entries());

        await context.SaveChangesAsync();

        await using var readContext = new GoldLedgerDbContext(options);

        var persistedOrder = await readContext.Orders.SingleAsync();

        Assert.Equal(OrderStatus.Pending, persistedOrder.Status);

        if (missingWallet)
        {
            Assert.Equal(0, await readContext.Wallets.CountAsync());
        }
        else
        {
            var persistedWallet = await readContext.Wallets.SingleAsync();

            Assert.Equal(5m, persistedWallet.Balance.Value);
            Assert.Equal(0m, persistedWallet.LockedBalance.Value);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Successful_sell_lifecycle_persists_order_and_wallet(
        bool cancel)
    {
        await using var connection =
            new SqliteConnection("Data Source=:memory:");

        await connection.OpenAsync();

        var options = CreateOptions(connection);

        await using var context = new GoldLedgerDbContext(options);

        await context.Database.EnsureCreatedAsync();

        var userId = Guid.NewGuid();

        context.Wallets.Add(
            GoldWallet.Create(
                userId,
                GoldWeight.Create(5m).Value!).Value!);

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var service = CreateService(context);

        var request = new CreateOrderRequestDto(
            userId,
            OrderType.Sell,
            1m,
            100m,
            1m,
            Array.Empty<string>(),
            null);

        var createResult = await service.CreateOrderAsync(request);

        Assert.True(createResult.IsSuccess);

        context.ChangeTracker.Clear();

        var pendingOrder = await context.Orders.SingleAsync();
        var lockedWallet = await context.Wallets.SingleAsync();

        Assert.Equal(OrderStatus.Pending, pendingOrder.Status);
        Assert.Equal(5m, lockedWallet.Balance.Value);
        Assert.Equal(1m, lockedWallet.LockedBalance.Value);
        Assert.Equal(4m, lockedWallet.AvailableBalance.Value);

        var orderId = pendingOrder.Id;

        context.ChangeTracker.Clear();

        var transitionResult = cancel
            ? await service.CancelOrderAsync(orderId)
            : await service.CompleteOrderAsync(orderId);

        Assert.True(transitionResult.IsSuccess);

        await using var readContext = new GoldLedgerDbContext(options);

        var persistedOrder = await readContext.Orders.SingleAsync();
        var persistedWallet = await readContext.Wallets.SingleAsync();

        Assert.Equal(
            cancel ? OrderStatus.Cancelled : OrderStatus.Settled,
            persistedOrder.Status);

        Assert.Equal(
            cancel ? 5m : 4m,
            persistedWallet.Balance.Value);

        Assert.Equal(0m, persistedWallet.LockedBalance.Value);

        var repeatedResult = cancel
            ? await service.CancelOrderAsync(orderId)
            : await service.CompleteOrderAsync(orderId);

        Assert.False(repeatedResult.IsSuccess);
        Assert.Equal(ErrorType.Validation, repeatedResult.ErrorType);
        Assert.Empty(context.ChangeTracker.Entries());

        await context.SaveChangesAsync();

        readContext.ChangeTracker.Clear();

        var walletAfterRetry = await readContext.Wallets.SingleAsync();

        Assert.Equal(
            cancel ? 5m : 4m,
            walletAfterRetry.Balance.Value);

        Assert.Equal(0m, walletAfterRetry.LockedBalance.Value);
    }

    private static OrderService CreateService(
        GoldLedgerDbContext context)
    {
        var unitOfWork = new UnitOfWork(
            context,
            NullLogger<UnitOfWork>.Instance);

        return new OrderService(
            new GoldOrderRepository(context),
            new GoldWalletRepository(context),
            unitOfWork);
    }

    private static DbContextOptions<GoldLedgerDbContext> CreateOptions(
        SqliteConnection connection)
    {
        return new DbContextOptionsBuilder<GoldLedgerDbContext>()
            .UseSqlite(connection)
            .Options;
    }
}
