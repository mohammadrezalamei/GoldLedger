using GoldLedger.Application.Contracts.Common;
using GoldLedger.Domain.Common;

namespace GoldLedger.Infrastructure.Persistence;

public sealed class UnitOfWork(GoldLedgerDbContext context) : IUnitOfWork
{
    public async Task<Result> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
        {
            return Result.Failure("رکورد همزمان توسط درخواست دیگری تغییر کرده است.", ErrorType.Conflict);
        }
        catch (Exception exception)
        {
            return Result.Failure(exception.Message, ErrorType.ServerError);
        }
    }
}

