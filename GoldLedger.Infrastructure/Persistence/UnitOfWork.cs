using GoldLedger.Application.Contracts.Common;
using GoldLedger.Domain.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GoldLedger.Infrastructure.Persistence;

public sealed class UnitOfWork(
    GoldLedgerDbContext context,
    ILogger<UnitOfWork> logger) : IUnitOfWork
{
    public async Task<Result> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        var committed = false;

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            await context.SaveChangesAsync(cancellationToken);

            committed = true;

            return Result.Success();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure(
                "اطلاعات هم‌زمان توسط درخواست دیگری تغییر کرده است؛ اطلاعات را دوباره دریافت کنید.",
                ErrorType.Conflict);
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is SqliteException
            {
                SqliteExtendedErrorCode: 2067
            })
        {
            return Result.Failure(
                "اطلاعات تکراری است؛ مقدار فیلدهای یکتا را بررسی کنید.",
                ErrorType.Conflict);
        }
        catch (DbUpdateException exception)
        {
            logger.LogError(
                exception,
                "Failed to save GoldLedger changes.");

            return Result.Failure(
                "ذخیره اطلاعات با خطا مواجه شد.",
                ErrorType.ServerError);
        }
        finally
        {
            if (!committed)
                DiscardChanges();
        }
    }

    public void DiscardChanges()
    {
        context.ChangeTracker.Clear();
    }
}
