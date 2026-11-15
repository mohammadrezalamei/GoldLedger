using GoldLedger.Application.Contracts.Common;
using GoldLedger.Domain.Common;
using GoldLedger.Infrastructure.Events;
using GoldLedger.Infrastructure.Payments;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GoldLedger.Infrastructure.Persistence;

public sealed class UnitOfWork(
    GoldLedgerDbContext context,
    ILogger<UnitOfWork> logger,
    IDomainEventDispatcher? dispatcher = null) : IUnitOfWork
{
    private readonly IDomainEventDispatcher _dispatcher =
        dispatcher ?? new DomainEventDispatcher(context, new PaymentSettlementService(context));

    public async Task<Result> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        var committed = false;

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var aggregates = context.ChangeTracker.Entries<AggregateRoot<Guid>>()
                .Where(entry => entry.State != EntityState.Deleted)
                .Select(entry => entry.Entity)
                .Where(aggregate => aggregate.DomainEvents.Count > 0)
                .ToArray();
            var domainEvents = aggregates.SelectMany(aggregate => aggregate.DomainEvents).ToArray();
            var dispatchResult = await _dispatcher.DispatchAsync(domainEvents, cancellationToken);
            if (!dispatchResult.IsSuccess)
                return dispatchResult;

            await context.SaveChangesAsync(cancellationToken);

            foreach (var aggregate in aggregates)
                aggregate.ClearDomainEvents();

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
                SqliteExtendedErrorCode: 2067 or 1555
            })
        {
            return Result.Failure(
                "اطلاعات تکراری است؛ مقدار فیلدهای یکتا را بررسی کنید.",
                ErrorType.Conflict);
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is SqliteException { SqliteErrorCode: 5 or 6 })
        {
            return Result.Failure(
                "پایگاه داده در حال پردازش درخواست دیگری است؛ دوباره تلاش کنید.",
                ErrorType.Conflict);
        }
        catch (SqliteException exception) when (exception.SqliteErrorCode is 5 or 6)
        {
            return Result.Failure(
                "پایگاه داده در حال پردازش درخواست دیگری است؛ دوباره تلاش کنید.",
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
        catch (SqliteException exception)
        {
            logger.LogError(exception, "Failed to prepare GoldLedger changes.");
            return Result.Failure("ذخیره اطلاعات با خطا مواجه شد.", ErrorType.ServerError);
        }
        finally
        {
            if (!committed)
                DiscardChanges();
        }
    }

    public void DiscardChanges()
    {
        foreach (var entry in context.ChangeTracker.Entries<AggregateRoot<Guid>>().ToArray())
            entry.Entity.ClearDomainEvents();

        context.ChangeTracker.Clear();
    }
}
