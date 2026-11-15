using GoldLedger.Domain.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace GoldLedger.API.Errors;

public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (context.Response.HasStarted)
            return false;

        if (exception is OperationCanceledException && context.RequestAborted.IsCancellationRequested)
        {
            context.Response.StatusCode = StatusCodes.Status499ClientClosedRequest;
            logger.LogDebug("Request cancelled by client. TraceId: {TraceId}", context.TraceIdentifier);
            return true;
        }

        var (statusCode, errorType, message) = MapException(exception);
        var result = Result.Failure(message, errorType);

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Request failed with status {StatusCode}. TraceId: {TraceId}",
                statusCode, context.TraceIdentifier);
        }
        else
        {
            logger.LogWarning(exception, "Request rejected with status {StatusCode}. TraceId: {TraceId}",
                statusCode, context.TraceIdentifier);
        }

        context.Response.StatusCode = statusCode;
        context.Response.Headers["X-Trace-Id"] = context.TraceIdentifier;
        await context.Response.WriteAsJsonAsync(result, cancellationToken);
        return true;
    }

    private static (int StatusCode, ErrorType ErrorType, string Message) MapException(Exception exception)
    {
        if (exception is BadHttpRequestException badRequest)
        {
            return (badRequest.StatusCode, ErrorType.Validation,
                "ساختار درخواست نامعتبر است.");
        }

        if (exception is DbUpdateConcurrencyException)
        {
            return (StatusCodes.Status409Conflict, ErrorType.Conflict,
                "داده‌ها توسط درخواست دیگری تغییر کرده‌اند؛ اطلاعات را تازه‌سازی و دوباره تلاش کنید.");
        }

        var sqliteException = exception switch
        {
            SqliteException sqlite => sqlite,
            DbUpdateException { InnerException: SqliteException sqlite } => sqlite,
            _ => null
        };

        if (sqliteException is { SqliteExtendedErrorCode: 1555 or 2067 })
        {
            return (StatusCodes.Status409Conflict, ErrorType.Conflict,
                "درخواست با داده‌ای که قبلاً ثبت شده است تداخل دارد.");
        }

        if (sqliteException is { SqliteErrorCode: 5 or 6 })
        {
            return (StatusCodes.Status409Conflict, ErrorType.Conflict,
                "داده‌ها در حال پردازش توسط درخواست دیگری هستند؛ دوباره تلاش کنید.");
        }

        return (StatusCodes.Status500InternalServerError, ErrorType.ServerError,
            "خطایی در پردازش درخواست رخ داد.");
    }
}
