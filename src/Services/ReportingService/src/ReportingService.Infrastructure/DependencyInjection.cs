using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ReportingService.Domain.Interfaces;
using ReportingService.Infrastructure.Messaging;
using ReportingService.Infrastructure.Persistence;
using ReportingService.Infrastructure.Repositories;

namespace ReportingService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<ReportingDbContext>(opt =>
            opt.UseSqlServer(config.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(ReportingDbContext).Assembly.FullName)));

        services.AddScoped<IDailySalesSummaryRepository, DailySalesSummaryRepository>();
        services.AddScoped<IMonthlyStationReportRepository, MonthlyStationReportRepository>();
        services.AddScoped<IGeneratedReportRepository, GeneratedReportRepository>();
        services.AddScoped<IScheduledReportRepository, ScheduledReportRepository>();
        services.AddScoped<IProcessedEventRepository, ProcessedEventRepository>();

        services.AddHostedService<ReportingConsumerHostedService>();
        services.AddHostedService<SignalRBridgeConsumerHostedService>();

        return services;
    }
}
