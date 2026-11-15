using GoldLedger.Domain.Aggregates.GoldWallet;
using GoldLedger.Domain.ValueObjects;
using GoldLedger.Infrastructure.Persistence;
using GoldLedger.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
namespace GoldLedger.Tests.Infrastructure;
public sealed class SqlitePersistenceTests
{
    [Fact]
    public async Task Wallet_round_trips_through_sqlite()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
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
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = CreateOptions(connection);
        var userId = Guid.NewGuid();
        await using var context = new GoldLedgerDbContext(options);
        await context.Database.EnsureCreatedAsync();
        context.Wallets.Add(GoldWallet.Create(userId).Value!);
        await context.SaveChangesAsync();
        context.Wallets.Add(GoldWallet.Create(userId).Value!);
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }
    [Fact]
    public async Task Order_tags_are_persisted()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = CreateOptions(connection);
        await using var context = new GoldLedgerDbContext(options);
        await context.Database.EnsureCreatedAsync();
        var tag = Tag.Create("long-term").Value!;
        var order = GoldLedger.Domain.Aggregates.GoldOrder.GoldOrder.Create(Guid.NewGuid(), OrderType.Buy, GoldWeight.Create(1m).Value!, Money.Create(100m).Value!, Percentage.Create(1m).Value!, [tag]).Value!;
        context.Orders.Add(order);
        await context.SaveChangesAsync();
        var persistedOrder = await context.Orders.Include(savedOrder => savedOrder.Tags).SingleAsync();
        Assert.Contains(persistedOrder.Tags, persistedTag => persistedTag.Value == "long-term");
    }
    private static DbContextOptions<GoldLedgerDbContext> CreateOptions(SqliteConnection connection)
    {
        return new DbContextOptionsBuilder<GoldLedgerDbContext>().UseSqlite(connection).Options;
    }
    private static async Task CreateWalletAsync(DbContextOptions<GoldLedgerDbContext> options, Guid userId, decimal balance)
    {
        await using var context = new GoldLedgerDbContext(options);
        await context.Database.EnsureCreatedAsync();
        var wallet = GoldWallet.Create(userId).Value!;
        wallet.Deposit(GoldWeight.Create(balance).Value!);
        context.Wallets.Add(wallet);
        await context.SaveChangesAsync();
    }
}


