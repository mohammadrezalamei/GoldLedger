using GoldLedger.Domain.ValueObjects;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace GoldLedger.API.Filters;

public sealed class IdempotencyOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.ApiDescription.HttpMethod is "GET" or "HEAD" or "OPTIONS")
            return;

        operation.Parameters ??= [];
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "Idempotency-Key",
            In = ParameterLocation.Header,
            Required = true,
            Description = "Unique per logical operation. Reuse the same key only when retrying identical input.",
            Schema = new OpenApiSchema
            {
                Type = JsonSchemaType.String,
                MinLength = 1,
                MaxLength = IdempotencyKey.MaxLength,
                Pattern = "^[A-Za-z0-9_.-]+$"
            }
        });
        operation.Responses ??= new OpenApiResponses();
        operation.Responses.TryAdd("409", new OpenApiResponse { Description = "Concurrency conflict or key reused with different input." });
        operation.Responses.TryAdd("422", new OpenApiResponse { Description = "Invalid domain input or missing/invalid idempotency key." });
    }
}
