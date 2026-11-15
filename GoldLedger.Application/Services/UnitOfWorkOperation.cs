using GoldLedger.Application.Contracts.Common;
using GoldLedger.Domain.Common;

namespace GoldLedger.Application.Services;

internal static class UnitOfWorkOperation
{
    public static async Task<TResult> ExecuteAsync<TResult>(
        IUnitOfWork unitOfWork,
        Func<Task<TResult>> operation,
        CancellationToken cancellationToken)
        where TResult : Result
    {
        var succeeded = false;

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = await operation();
            succeeded = result.IsSuccess;
            return result;
        }
        finally
        {
            if (!succeeded)
                unitOfWork.DiscardChanges();
        }
    }
}
