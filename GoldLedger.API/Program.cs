using GoldLedger.API.Errors;
using GoldLedger.API.Filters;
using GoldLedger.Application.Contracts.Services;
using GoldLedger.Application.Services;
using GoldLedger.Infrastructure;

namespace GoldLedger.API;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers(options => options.Filters.Add<IdempotencyFilter>())
            .ConfigureApiBehaviorOptions(options =>
                options.InvalidModelStateResponseFactory = _ =>
                    new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(
                        GoldLedger.Domain.Common.Result.Failure(
                            "ساختار درخواست نامعتبر است.", GoldLedger.Domain.Common.ErrorType.Validation)));
        builder.Services.AddExceptionHandler<ApiExceptionHandler>();
        builder.Services.AddProblemDetails();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options => options.OperationFilter<IdempotencyOperationFilter>());

        var connectionString =
            builder.Configuration.GetConnectionString("GoldLedger")
            ?? throw new InvalidOperationException(
                "Connection string 'GoldLedger' is not configured.");

        var databaseSettings = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder(connectionString);
        if (databaseSettings.DataSource != ":memory:" && !Path.IsPathRooted(databaseSettings.DataSource))
            databaseSettings.DataSource = Path.GetFullPath(databaseSettings.DataSource, builder.Environment.ContentRootPath);

        builder.Services.AddInfrastructure(databaseSettings.ToString());

        builder.Services.AddScoped<IWalletService, WalletService>();
        builder.Services.AddScoped<IOrderService, OrderService>();

        var app = builder.Build();

        app.UseExceptionHandler();
        app.UseSwagger();

        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint(
                "/swagger/v1/swagger.json",
                "GoldLedger API v1");

            options.RoutePrefix = string.Empty;
        });

        app.MapControllers();

        app.Run();
    }
}
