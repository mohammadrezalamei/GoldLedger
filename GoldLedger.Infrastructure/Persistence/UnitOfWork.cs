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
            return Result.Failure("Ø±Ú©ÙˆØ±Ø¯ Ù‡Ù…Ø²Ù…Ø§Ù† ØªÙˆØ³Ø· Ø¯Ø±Ø®ÙˆØ§Ø³Øª Ø¯ÛŒÚ¯Ø±ÛŒ ØªØºÛŒÛŒØ± Ú©Ø±Ø¯Ù‡ Ø§Ø³Øª.", ErrorType.Conflict);
        }
        catch (Exception exception)
        {
            return Result.Failure(exception.Message, ErrorType.ServerError);
        }
    }
}


