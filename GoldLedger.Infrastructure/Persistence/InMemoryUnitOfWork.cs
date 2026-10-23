using GoldLedger.Application.Contracts.Common;
using GoldLedger.Domain.Common;

namespace GoldLedger.Infrastructure.Persistence;

public class InMemoryUnitOfWork : IUnitOfWork
{
    public Task<Result> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Success());
    }
}