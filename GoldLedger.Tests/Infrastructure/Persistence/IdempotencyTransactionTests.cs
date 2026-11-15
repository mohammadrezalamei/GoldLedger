using GoldLedger.Application.Contracts.Services;
using GoldLedger.Domain.Aggregates.GoldWallet;
using GoldLedger.Domain.ValueObjects;
using GoldLedger.Infrastructure.Persistence;
using GoldLedger.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;

namespace GoldLedger.Tests.Infrastructure.Persistence;


public sealed class IdempotencyTransactionTests
{
    [Theory]
    [InlineData(422)]
    [InlineData(409)]
    [InlineData(500)]
    public async Task Failed_response_rolls_back_even_changes_already_saved_by_action(int statusCode)
    {
        await using var database = new SqliteTestDatabase();
        await database.InitializeAsync();
        await using var context = database.CreateContext();
        var service = CreateService(context);

        var result = await service.ExecuteAsync(Key(), "request", async () =>
        {
            context.Wallets.Add(GoldWallet.Create(Guid.NewGuid()).Value!);
            await context.SaveChangesAsync();
            return new IdempotencyResponse(statusCode, "failure", "application/json");
        });

        Assert.Equal(statusCode, result.StatusCode);
        Assert.Empty(context.ChangeTracker.Entries());
        await using var verification = database.CreateContext();
        Assert.Empty(await verification.Wallets.ToListAsync());
        Assert.Empty(await verification.IdempotencyRecords.ToListAsync());
    }

    [Fact]
    public async Task Exception_after_saved_business_changes_rolls_back_everything()
    {
        await using var database = new SqliteTestDatabase();
        await database.InitializeAsync();
        await using var context = database.CreateContext();

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await CreateService(context).ExecuteAsync(Key(), "request", async () =>
            {
                context.Wallets.Add(GoldWallet.Create(Guid.NewGuid()).Value!);
                await context.SaveChangesAsync();
                throw new InvalidOperationException("Injected response failure");
            });
        });

        Assert.Empty(context.ChangeTracker.Entries());
        await using var verification = database.CreateContext();
        Assert.Empty(await verification.Wallets.ToListAsync());
        Assert.Empty(await verification.IdempotencyRecords.ToListAsync());
    }

    [Fact]
    public async Task Cancellation_after_saved_business_changes_rolls_back_everything()
    {
        await using var database = new SqliteTestDatabase();
        await database.InitializeAsync();
        await using var context = database.CreateContext();
        using var cancellation = new CancellationTokenSource();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await CreateService(context).ExecuteAsync(Key(), "request", async () =>
            {
                context.Wallets.Add(GoldWallet.Create(Guid.NewGuid()).Value!);
                await context.SaveChangesAsync();
                cancellation.Cancel();
                return new IdempotencyResponse(201, "created", "application/json");
            }, cancellation.Token);
        });

        Assert.Empty(context.ChangeTracker.Entries());
        await using var verification = database.CreateContext();
        Assert.Empty(await verification.Wallets.ToListAsync());
        Assert.Empty(await verification.IdempotencyRecords.ToListAsync());
    }

    [Fact]
    public async Task Committed_response_survives_new_context_and_preserves_location_and_content_type()
    {
        await using var database = new SqliteTestDatabase();
        await database.InitializeAsync();
        var key = Key();
        var expected = new IdempotencyResponse(201, "{\"value\":1}", "application/json; charset=utf-8", "/api/orders/1");
        await using (var context = database.CreateContext())
        {
            await CreateService(context).ExecuteAsync(key, "request", async () =>
            {
                context.Wallets.Add(GoldWallet.Create(Guid.NewGuid()).Value!);
                await context.SaveChangesAsync();
                return expected;
            });
        }
        await using var replayContext = database.CreateContext();
        var executedAgain = false;

        var replay = await CreateService(replayContext).ExecuteAsync(key, "request", () =>
        {
            executedAgain = true;
            return Task.FromResult(expected);
        });

        Assert.False(executedAgain);
        Assert.Equal(expected with { Replayed = true }, replay);
        Assert.Single(await replayContext.Wallets.ToListAsync());
        Assert.Single(await replayContext.IdempotencyRecords.ToListAsync());
    }

    [Fact]
    public async Task Mismatched_request_hash_does_not_execute_or_replace_original_record()
    {
        await using var database = new SqliteTestDatabase();
        await database.InitializeAsync();
        await using var context = database.CreateContext();
        var key = Key();
        var response = new IdempotencyResponse(200, "original", "application/json");
        await CreateService(context).ExecuteAsync(key, "first", () => Task.FromResult(response));
        var executed = false;

        var conflict = await CreateService(context).ExecuteAsync(key, "second", () =>
        {
            executed = true;
            return Task.FromResult(response);
        });

        Assert.Equal(409, conflict.StatusCode);
        Assert.False(executed);
        var record = await context.IdempotencyRecords.SingleAsync();
        Assert.Equal("first", record.RequestHash);
        Assert.Equal("original", record.Body);
    }

    private static IdempotencyKey Key()
    {
        return IdempotencyKey.Create(Guid.NewGuid().ToString("N")).Value!;
    }

    private static IdempotencyService CreateService(GoldLedgerDbContext context)
    {
        return new IdempotencyService(context, SqliteTestDatabase.CreateUnitOfWork(context),
            new TehranClock(TimeProvider.System));
    }
}
