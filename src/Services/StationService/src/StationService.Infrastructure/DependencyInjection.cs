using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StationService.Application.Interfaces;
using StationService.Domain.Interfaces;
using StationService.Infrastructure.Messaging;
using StationService.Infrastructure.Persistence;
using StationService.Infrastructure.Repositories;

namespace StationService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<StationsDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(StationsDbContext).Assembly.FullName)));

        services.AddScoped<IStationRepository, StationRepository>();
        services.AddScoped<IFuelTypeRepository, FuelTypeRepository>();
        services.AddScoped<IProcessedEventRepository, ProcessedEventRepository>();

        services.AddSingleton<IRabbitMqPublisher, RabbitMqPublisher>();

        return services;
    }
}
