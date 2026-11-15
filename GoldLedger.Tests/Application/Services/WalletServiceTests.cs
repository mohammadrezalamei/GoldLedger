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

namespace GoldLedger.Tests.Application.Services;

public sealed class WalletServiceTests
{
    [Fact]
    public async Task Business_failure_discards_uncommitted_wallet_changes()
    {
        await using var database = new SqliteTestDatabase();
        await database.InitializeAsync();
        var userId = await database.SeedWalletAsync();
        await using var context = database.CreateContext();
        var wallet = await context.Wallets.SingleAsync();
        wallet.Deposit(GoldWeight.Create(1m).Value!);
        var service = new WalletService(new GoldWalletRepository(context), SqliteTestDatabase.CreateUnitOfWork(context));

        var failure = await service.WithdrawAsync(userId, new(decimal.MaxValue));

        Assert.False(failure.IsSuccess);
        Assert.Empty(context.ChangeTracker.Entries());
        await context.SaveChangesAsync();
        await using var verification = database.CreateContext();
        Assert.Equal(5m, (await verification.Wallets.SingleAsync()).Balance.Value);
    }
}
