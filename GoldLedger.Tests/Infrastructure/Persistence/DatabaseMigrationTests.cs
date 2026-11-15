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

public sealed class DatabaseMigrationTests
{
    [Fact]
    public async Task Upgrade_from_initial_schema_preserves_wallet_and_matches_current_model()
    {
        await using var database = new SqliteTestDatabase();
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
}
