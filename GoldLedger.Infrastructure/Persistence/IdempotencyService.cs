using System.Text.Json;
using GoldLedger.Application.Contracts.Common;
using GoldLedger.Application.Contracts.Services;
using GoldLedger.Domain.Common;
using GoldLedger.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace GoldLedger.Infrastructure.Persistence;

public sealed class IdempotencyService(
    GoldLedgerDbContext context,
    IUnitOfWork unitOfWork,
    IClock clock) : IIdempotencyService
{
    public async Task<IdempotencyResponse> ExecuteAsync(
        IdempotencyKey key,
        string requestHash,
        Func<Task<IdempotencyResponse>> operation,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var committed = false;

        try
        {
            var previous = await context.IdempotencyRecords.AsNoTracking()
                .SingleOrDefaultAsync(record => record.Key == key.Value, cancellationToken);
            if (previous is not null)
            {
                if (previous.RequestHash != requestHash)
                {
                    var failure = Result.Failure("این کلید قبلاً برای درخواست متفاوتی استفاده شده است.", ErrorType.Conflict);
                    return new IdempotencyResponse(409,
                        JsonSerializer.Serialize(failure, JsonSerializerOptions.Web), "application/json; charset=utf-8");
                }

                return new IdempotencyResponse(previous.StatusCode, previous.Body,
                    previous.ContentType, previous.Location, Replayed: true);
            }

            var response = await operation();
            if (response.StatusCode is < 200 or >= 300)
                return response;

            context.IdempotencyRecords.Add(new IdempotencyRecord(
                key.Value, requestHash, response.StatusCode, response.Body,
                response.ContentType, response.Location, clock.Now.UtcDateTime));

            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            committed = true;
            return response;
        }
        finally
        {
            if (!committed)
            {
                try
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                }
                finally
                {
                    unitOfWork.DiscardChanges();
                }
            }
        }
    }
}
