using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using InventoryService.Application.Interfaces;
using InventoryService.Domain.Interfaces;
using InventoryService.Infrastructure.Messaging;
using InventoryService.Infrastructure.Persistence;
using InventoryService.Infrastructure.Repositories;

namespace InventoryService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<InventoryDbContext>(opt =>
            opt.UseSqlServer(config.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(InventoryDbContext).Assembly.FullName)));

        services.AddScoped<ITankRepository, TankRepository>();
        services.AddScoped<IStockLoadingRepository, StockLoadingRepository>();
        services.AddScoped<IDipReadingRepository, DipReadingRepository>();
        services.AddScoped<IReplenishmentRequestRepository, ReplenishmentRequestRepository>();
        services.AddScoped<IProcessedEventRepository, ProcessedEventRepository>();

        services.AddSingleton<IRabbitMqPublisher, RabbitMqPublisher>();
        services.AddHostedService<SagaConsumerHostedService>();

        return services;
    }
}
