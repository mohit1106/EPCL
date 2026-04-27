using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SalesService.Application.Interfaces;
using SalesService.Domain.Interfaces;
using SalesService.Infrastructure.Messaging;
using SalesService.Infrastructure.Persistence;
using SalesService.Infrastructure.Repositories;
using SalesService.Infrastructure.Services;

namespace SalesService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<SalesDbContext>(opt =>
            opt.UseSqlServer(config.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(SalesDbContext).Assembly.FullName)));

        // Repositories
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<IPumpRepository, PumpRepository>();
        services.AddScoped<IFuelPriceRepository, FuelPriceRepository>();
        services.AddScoped<IShiftRepository, ShiftRepository>();
        services.AddScoped<IVoidedTransactionRepository, VoidedTransactionRepository>();
        services.AddScoped<IRegisteredVehicleRepository, RegisteredVehicleRepository>();
        services.AddScoped<IFleetAccountRepository, FleetAccountRepository>();
        services.AddScoped<IFleetVehicleRepository, FleetVehicleRepository>();
        services.AddScoped<ICustomerWalletRepository, CustomerWalletRepository>();
        services.AddScoped<IWalletTransactionRepository, WalletTransactionRepository>();
        services.AddScoped<IProcessedEventRepository, ProcessedEventRepository>();
        services.AddScoped<IFuelPreAuthorizationRepository, FuelPreAuthorizationRepository>();

        // Messaging
        services.AddSingleton<IRabbitMqPublisher, RabbitMqPublisher>();
        services.AddHostedService<SagaConsumerHostedService>();

        // Razorpay
        services.Configure<RazorpaySettings>(config.GetSection("Razorpay"));
        services.AddSingleton<IRazorpayService, RazorpayService>();

        return services;
    }
}
