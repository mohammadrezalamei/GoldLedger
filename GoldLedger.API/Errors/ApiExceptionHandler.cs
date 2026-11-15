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
        if (exception is OperationCanceledException && context.RequestAborted.IsCancellationRequested)
        {
            context.Response.StatusCode = StatusCodes.Status499ClientClosedRequest;
            return true;
        }

        var sqliteException = exception as SqliteException ?? exception.InnerException as SqliteException;
        var conflict = exception is DbUpdateConcurrencyException ||
            sqliteException is { SqliteErrorCode: 5 or 6 } ||
            sqliteException is { SqliteExtendedErrorCode: 1555 or 2067 };
        var badRequest = exception is BadHttpRequestException;
        var statusCode = conflict ? StatusCodes.Status409Conflict
            : badRequest ? ((BadHttpRequestException)exception).StatusCode
            : StatusCodes.Status500InternalServerError;
        var errorType = conflict ? ErrorType.Conflict
            : badRequest ? ErrorType.Validation
            : ErrorType.ServerError;
        var message = conflict ? "درخواست با تغییر هم‌زمان یا داده تکراری تداخل دارد؛ دوباره تلاش کنید."
            : badRequest ? "ساختار درخواست نامعتبر است."
            : "خطایی در پردازش درخواست رخ داد.";

        logger.LogError(exception, "Request failed. TraceId: {TraceId}", context.TraceIdentifier);
        context.Response.StatusCode = statusCode;
        context.Response.Headers["X-Trace-Id"] = context.TraceIdentifier;
        await context.Response.WriteAsJsonAsync(Result.Failure(message, errorType), cancellationToken);
        return true;
    }
}
