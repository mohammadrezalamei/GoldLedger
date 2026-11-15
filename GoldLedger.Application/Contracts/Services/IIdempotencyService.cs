using GoldLedger.Domain.ValueObjects;

namespace GoldLedger.Application.Contracts.Services;

public interface IIdempotencyService
{
    Task<IdempotencyResponse> ExecuteAsync(
        IdempotencyKey key,
        string requestHash,
        Func<Task<IdempotencyResponse>> operation,
        CancellationToken cancellationToken = default);
}

public sealed record IdempotencyResponse(
    int StatusCode,
    string Body,
    string ContentType,
    string? Location = null,
    bool Replayed = false);
