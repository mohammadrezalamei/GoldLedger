using GoldLedger.Application.Services;
using Microsoft.Data.Sqlite;
using GoldLedger.Domain.Aggregates.GoldWallet;
using GoldLedger.Domain.ValueObjects;
using GoldLedger.Infrastructure.Persistence;
using GoldLedger.Infrastructure.Repositories;
using GoldLedger.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace GoldLedger.Tests.Infrastructure;

internal sealed class SqliteTestDatabase : IAsyncDisposable
{
    private readonly string databasePath = Path.Combine(Path.GetTempPath(), $"ledger-integrity-{Guid.NewGuid():N}.db");

    public GoldLedgerDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<GoldLedgerDbContext>()
            .UseSqlite($"Data Source={databasePath};Pooling=False;Default Timeout=10").Options;
        return new GoldLedgerDbContext(options);
    }

    public async Task InitializeAsync()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public async Task<Guid> SeedWalletAsync(decimal balance = 5m)
    {
        var userId = Guid.NewGuid();
        await using var context = CreateContext();
        context.Wallets.Add(GoldWallet.Create(userId, GoldWeight.Create(balance).Value!).Value!);
        await context.SaveChangesAsync();
        return userId;
    }

    public static UnitOfWork CreateUnitOfWork(GoldLedgerDbContext context)
    {
        return new UnitOfWork(context, NullLogger<UnitOfWork>.Instance);
    }

    public static OrderService CreateOrderService(GoldLedgerDbContext context)
    {
        return new OrderService(new GoldOrderRepository(context), new GoldWalletRepository(context),
            CreateUnitOfWork(context), new TehranClock(TimeProvider.System));
    }

    public ValueTask DisposeAsync()
    {
        if (File.Exists(databasePath))
            File.Delete(databasePath);
        return ValueTask.CompletedTask;
    }

    public static DbContextOptions<GoldLedgerDbContext> CreateOptions(
        SqliteConnection connection)
    {
        return new DbContextOptionsBuilder<GoldLedgerDbContext>()
            .UseSqlite(connection)
            .Options;
    }

    public static async Task CreateWalletAsync(
        DbContextOptions<GoldLedgerDbContext> options,
        Guid userId,
        decimal balance)
    {
        await using var context = new GoldLedgerDbContext(options);

        await context.Database.EnsureCreatedAsync();

        var wallet = GoldWallet.Create(userId).Value!;

        wallet.Deposit(GoldWeight.Create(balance).Value!);

        context.Wallets.Add(wallet);

        await context.SaveChangesAsync();
    }
}
