using GoldLedger.Domain.Aggregates.GoldWallet;
using GoldLedger.Domain.Common;
using GoldLedger.Domain.ValueObjects;
using GoldLedger.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using static GoldLedger.Tests.Infrastructure.SqliteTestDatabase;

namespace GoldLedger.Tests.Infrastructure.Persistence;

public sealed class GoldOrderPersistenceTests
{
    [Fact]
    public async Task Order_tags_are_persisted()
    {
        await using var connection =
            new SqliteConnection("Data Source=:memory:");

        await connection.OpenAsync();

        var options = CreateOptions(connection);

        await using var context = new GoldLedgerDbContext(options);

        await context.Database.EnsureCreatedAsync();

        var tag = Tag.Create("long-term").Value!;

        var order =
            GoldLedger.Domain.Aggregates.GoldOrder.GoldOrder.Create(
                Guid.NewGuid(),
                OrderType.Buy,
                GoldWeight.Create(1m).Value!,
                Money.Create(100m).Value!,
                Percentage.Create(1m).Value!,
                [tag]).Value!;

        context.Orders.Add(order);

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var persistedOrder = await context.Orders
            .Include(savedOrder => savedOrder.Tags)
            .SingleAsync();

        Assert.Contains(
            persistedOrder.Tags,
            persistedTag => persistedTag.Value == "long-term");
    }
}
