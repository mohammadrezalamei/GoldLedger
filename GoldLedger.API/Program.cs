using GoldLedger.Application.Contracts.Services;
using GoldLedger.Application.Services;
using GoldLedger.Infrastructure;

namespace GoldLedger.API;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        var connectionString =
            builder.Configuration.GetConnectionString("GoldLedger")
            ?? throw new InvalidOperationException(
                "Connection string 'GoldLedger' is not configured.");

        builder.Services.AddInfrastructure(connectionString);

        builder.Services.AddScoped<IWalletService, WalletService>();
        builder.Services.AddScoped<IOrderService, OrderService>();

        var app = builder.Build();

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
