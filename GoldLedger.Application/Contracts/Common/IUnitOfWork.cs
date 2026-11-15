using GoldLedger.Domain.Common;

namespace GoldLedger.Application.Contracts.Common;

public interface IUnitOfWork
{
    Task<Result> SaveChangesAsync(
        CancellationToken cancellationToken = default);

    void DiscardChanges();
}
