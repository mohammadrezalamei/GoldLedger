using GoldLedger.Domain.Aggregates.GoldWallet;
using GoldLedger.Domain.Common;
using GoldLedger.Domain.ValueObjects;
using GoldLedger.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using static GoldLedger.Tests.Infrastructure.SqliteTestDatabase;

namespace GoldLedger.Tests.Infrastructure.Persistence;

public sealed class UnitOfWorkTests
{
    [Fact]
    public async Task Unit_of_work_maps_stale_update_to_conflict_result()
    {
        var databasePath = Path.Combine(
            Path.GetTempPath(),
            $"gold-ledger-{Guid.NewGuid():N}.db");

        try
        {
            var options = new DbContextOptionsBuilder<GoldLedgerDbContext>()
                .UseSqlite($"Data Source={databasePath}")
                .Options;

            var userId = Guid.NewGuid();

            await using (var setupContext = new GoldLedgerDbContext(options))
            {
                await setupContext.Database.EnsureCreatedAsync();

                setupContext.Wallets.Add(
                    GoldWallet.Create(
                        userId,
                        GoldWeight.Create(4m).Value!).Value!);

                await setupContext.SaveChangesAsync();
            }

            await using var firstContext = new GoldLedgerDbContext(options);
            await using var secondContext = new GoldLedgerDbContext(options);

            var firstWallet = await firstContext.Wallets.SingleAsync();
            var secondWallet = await secondContext.Wallets.SingleAsync();

            firstWallet.Deposit(GoldWeight.Create(1m).Value!);
            await firstContext.SaveChangesAsync();

            secondWallet.Deposit(GoldWeight.Create(1m).Value!);

            var unitOfWork = new UnitOfWork(
                secondContext,
                NullLogger<UnitOfWork>.Instance);

            var result = await unitOfWork.SaveChangesAsync();

            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorType.Conflict, result.ErrorType);
            Assert.Empty(secondContext.ChangeTracker.Entries());
        }
        finally
        {
            try
            {
                if (File.Exists(databasePath))
                    File.Delete(databasePath);
            }
            catch (IOException)
            {
            }
        }
    }

    [Fact]
    public async Task Unit_of_work_maps_duplicate_wallet_to_conflict_and_discards_changes()
    {
        await using var connection =
            new SqliteConnection("Data Source=:memory:");

        await connection.OpenAsync();

        await using var context =
            new GoldLedgerDbContext(CreateOptions(connection));

        await context.Database.EnsureCreatedAsync();

        var userId = Guid.NewGuid();

        context.Wallets.Add(GoldWallet.Create(userId).Value!);
        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        context.Wallets.Add(GoldWallet.Create(userId).Value!);

        var unitOfWork = new UnitOfWork(
            context,
            NullLogger<UnitOfWork>.Instance);

        var result = await unitOfWork.SaveChangesAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Empty(context.ChangeTracker.Entries());

        Assert.Equal(1, await context.Wallets.CountAsync());
    }

    [Fact]
    public async Task Unit_of_work_propagates_cancellation_and_discards_changes()
    {
        await using var connection =
            new SqliteConnection("Data Source=:memory:");

        await connection.OpenAsync();

        await using var context =
            new GoldLedgerDbContext(CreateOptions(connection));

        await context.Database.EnsureCreatedAsync();

        context.Wallets.Add(
            GoldWallet.Create(Guid.NewGuid()).Value!);

        var unitOfWork = new UnitOfWork(
            context,
            NullLogger<UnitOfWork>.Instance);

        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => unitOfWork.SaveChangesAsync(cancellationSource.Token));

        Assert.Empty(context.ChangeTracker.Entries());
        Assert.Equal(0, await context.Wallets.CountAsync());
    }
}
