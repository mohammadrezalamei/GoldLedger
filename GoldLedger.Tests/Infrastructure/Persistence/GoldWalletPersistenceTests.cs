using GoldLedger.Domain.Aggregates.GoldWallet;
using GoldLedger.Domain.Common;
using GoldLedger.Domain.ValueObjects;
using GoldLedger.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using static GoldLedger.Tests.Infrastructure.SqliteTestDatabase;

namespace GoldLedger.Tests.Infrastructure.Persistence;

public sealed class GoldWalletPersistenceTests
{
    [Fact]
    public async Task Wallet_round_trips_through_sqlite()
    {
        await using var connection =
            new SqliteConnection("Data Source=:memory:");

        await connection.OpenAsync();

        var options = CreateOptions(connection);
        var userId = Guid.NewGuid();

        await CreateWalletAsync(options, userId, 3m);

        await using var readContext = new GoldLedgerDbContext(options);

        var persistedWallet = await readContext.Wallets.SingleAsync();

        Assert.Equal(userId, persistedWallet.UserId);
        Assert.Equal(3m, persistedWallet.Balance.Value);
    }

    [Fact]
    public async Task Wallet_user_id_is_unique()
    {
        await using var connection =
            new SqliteConnection("Data Source=:memory:");

        await connection.OpenAsync();

        var options = CreateOptions(connection);
        var userId = Guid.NewGuid();

        await using var context = new GoldLedgerDbContext(options);

        await context.Database.EnsureCreatedAsync();

        context.Wallets.Add(GoldWallet.Create(userId).Value!);
        await context.SaveChangesAsync();

        context.Wallets.Add(GoldWallet.Create(userId).Value!);

        await Assert.ThrowsAsync<DbUpdateException>(
            () => context.SaveChangesAsync());
    }

    [Fact]
    public async Task Concurrent_wallet_updates_allow_first_save_and_reject_stale_save()
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
                        GoldWeight.Create(5m).Value!).Value!);

                await setupContext.SaveChangesAsync();
            }

            await using var firstContext = new GoldLedgerDbContext(options);
            await using var secondContext = new GoldLedgerDbContext(options);

            var firstWallet = await firstContext.Wallets.SingleAsync();
            var secondWallet = await secondContext.Wallets.SingleAsync();

            Assert.Equal(
                firstWallet.ConcurrencyToken,
                secondWallet.ConcurrencyToken);

            firstWallet.Deposit(GoldWeight.Create(1m).Value!);
            await firstContext.SaveChangesAsync();

            secondWallet.Deposit(GoldWeight.Create(1m).Value!);

            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
                () => secondContext.SaveChangesAsync());
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
    public async Task Concurrency_token_is_incremented_after_successful_update()
    {
        await using var connection =
            new SqliteConnection("Data Source=:memory:");

        await connection.OpenAsync();

        var options = CreateOptions(connection);
        var userId = Guid.NewGuid();

        await CreateWalletAsync(options, userId, 3m);

        await using var context = new GoldLedgerDbContext(options);

        var wallet = await context.Wallets.SingleAsync();
        var initialToken = wallet.ConcurrencyToken;

        wallet.Deposit(GoldWeight.Create(1m).Value!);
        await context.SaveChangesAsync();

        Assert.Equal(initialToken + 1, wallet.ConcurrencyToken);
    }

    [Fact]
    public async Task Database_check_constraints_reject_negative_wallet_balance()
    {
        await using var connection =
            new SqliteConnection("Data Source=:memory:");

        await connection.OpenAsync();

        var options = CreateOptions(connection);

        await using var context = new GoldLedgerDbContext(options);

        await context.Database.EnsureCreatedAsync();

        var wallet = GoldWallet.Create(Guid.NewGuid()).Value!;

        context.Wallets.Add(wallet);
        await context.SaveChangesAsync();

        await Assert.ThrowsAsync<SqliteException>(
            () => context.Database.ExecuteSqlRawAsync(
                "UPDATE Wallets SET Balance = -1 WHERE Id = {0}",
                wallet.Id));
    }
}
