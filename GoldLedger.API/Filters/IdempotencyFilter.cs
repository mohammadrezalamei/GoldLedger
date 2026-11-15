using System.Runtime.ExceptionServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GoldLedger.API.Extensions;
using GoldLedger.Application.Contracts.Services;
using GoldLedger.Domain.ValueObjects;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace GoldLedger.API.Filters;

public sealed class IdempotencyFilter(
    IIdempotencyService idempotencyService,
    IOptions<JsonOptions> jsonOptions) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var request = context.HttpContext.Request;
        if (HttpMethods.IsGet(request.Method) || HttpMethods.IsHead(request.Method) || HttpMethods.IsOptions(request.Method))
        {
            await next();
            return;
        }

        var keyResult = IdempotencyKey.Create(request.Headers["Idempotency-Key"].ToString());
        if (!keyResult.IsSuccess)
        {
            context.Result = keyResult.ToActionResult();
            return;
        }

        var arguments = context.ActionArguments
            .Where(argument => argument.Value is not CancellationToken)
            .OrderBy(argument => argument.Key)
            .ToDictionary(argument => argument.Key, argument => argument.Value);
        var payload = JsonSerializer.Serialize(new
        {
            Method = request.Method,
            Path = request.Path.Value?.ToLowerInvariant(),
            Arguments = arguments
        }, jsonOptions.Value.JsonSerializerOptions);
        var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));

        ActionExecutedContext? actionContext = null;
        var response = await idempotencyService.ExecuteAsync(keyResult.Value!, fingerprint, async () =>
        {
            var executed = await next();
            actionContext = executed;
            if (executed.Exception is not null && !executed.ExceptionHandled)
                ExceptionDispatchInfo.Capture(executed.Exception).Throw();

            if (executed.Result is not ObjectResult objectResult)
                throw new InvalidOperationException("Mutation endpoints must return an object response.");

            var body = JsonSerializer.Serialize(objectResult.Value,
                objectResult.Value?.GetType() ?? typeof(object), jsonOptions.Value.JsonSerializerOptions);
            var location = objectResult is CreatedResult created ? created.Location : null;

            return new IdempotencyResponse(objectResult.StatusCode ?? StatusCodes.Status200OK,
                body, "application/json; charset=utf-8", location);
        }, context.HttpContext.RequestAborted);

        context.HttpContext.Response.Headers["Idempotency-Replayed"] = response.Replayed ? "true" : "false";
        if (!string.IsNullOrEmpty(response.Location))
            context.HttpContext.Response.Headers.Location = response.Location;

        var result = new ContentResult
        {
            StatusCode = response.StatusCode,
            ContentType = response.ContentType,
            Content = response.Body
        };

        if (actionContext is null)
            context.Result = result;
        else
            actionContext.Result = result;
    }
}
