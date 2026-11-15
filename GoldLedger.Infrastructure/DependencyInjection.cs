using GoldLedger.Application.Contracts.Common;
using GoldLedger.Application.Contracts.Services;
using GoldLedger.Domain.Repositories;
using GoldLedger.Infrastructure.Payments;
using GoldLedger.Infrastructure.Events;
using GoldLedger.Infrastructure.Persistence;
using GoldLedger.Infrastructure.Repositories;
using GoldLedger.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GoldLedger.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddDbContext<GoldLedgerDbContext>(options =>
            options.UseSqlite(connectionString));

        services.AddScoped<IGoldWalletRepository, GoldWalletRepository>();
        services.AddScoped<IGoldOrderRepository, GoldOrderRepository>();

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IClock, TehranClock>();
        services.AddScoped<IPaymentSettlementService, PaymentSettlementService>();

        return services;
    }
}
