using GoldLedger.Domain.Common;

namespace GoldLedger.Application.Contracts.Common
{
    public interface IUnitOfWork
    {
        public Task<Result> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}

