using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AuditService.Domain.Interfaces;
using AuditService.Infrastructure.Messaging;
using AuditService.Infrastructure.Persistence;
using AuditService.Infrastructure.Repositories;

namespace AuditService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<AuditDbContext>(opt =>
            opt.UseSqlServer(config.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(AuditDbContext).Assembly.FullName)));

        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddHostedService<AuditEventConsumerHostedService>();

        return services;
    }
}
