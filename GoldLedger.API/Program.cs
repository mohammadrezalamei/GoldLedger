using GoldLedger.Application.Contracts.Common;
using GoldLedger.Application.Contracts.Services;
using GoldLedger.Application.Services;
using GoldLedger.Domain.Repositories;
using GoldLedger.Infrastructure.Persistence;
using GoldLedger.Infrastructure.Repositories;

namespace GoldLedger.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddSingleton<IGoldWalletRepository, InMemoryGoldWalletRepository>();
            builder.Services.AddSingleton<IGoldOrderRepository, InMemoryGoldOrderRepository>();
            builder.Services.AddSingleton<IUnitOfWork, InMemoryUnitOfWork>();

            builder.Services.AddScoped<IWalletService, WalletService>();
            builder.Services.AddScoped<IOrderService, OrderService>();

            var app = builder.Build();
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "GoldLedger API v1");
                c.RoutePrefix = string.Empty;
            });
            app.MapControllers();

            app.Run();
        }
    }
}
