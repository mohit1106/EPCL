using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using FraudDetectionService.Application.Interfaces;
using FraudDetectionService.Application.Rules;
using FraudDetectionService.Domain.Interfaces;
using FraudDetectionService.Infrastructure.Messaging;
using FraudDetectionService.Infrastructure.Persistence;
using FraudDetectionService.Infrastructure.Repositories;

namespace FraudDetectionService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<FraudDbContext>(opt =>
            opt.UseSqlServer(config.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(FraudDbContext).Assembly.FullName)));

        // Repositories
        services.AddScoped<IFraudAlertRepository, FraudAlertRepository>();
        services.AddScoped<IFraudRuleEvaluationRepository, FraudRuleEvaluationRepository>();
        services.AddScoped<IProcessedEventRepository, ProcessedEventRepository>();

        // Messaging
        services.AddSingleton<IRabbitMqPublisher, RabbitMqPublisher>();
        services.AddHostedService<FraudConsumerHostedService>();

        // Register all 10 fraud rules
        services.AddScoped<IFraudRule, OversellRule>();
        services.AddScoped<IFraudRule, RapidTransactionRule>();
        services.AddScoped<IFraudRule, DipVarianceRule>();
        services.AddScoped<IFraudRule, OddHoursRule>();
        services.AddScoped<IFraudRule, RoundNumberRule>();
        services.AddScoped<IFraudRule, PriceMismatchRule>();
        services.AddScoped<IFraudRule, DuplicateTransactionRule>();
        services.AddScoped<IFraudRule, VolumeSpikeRule>();
        services.AddScoped<IFraudRule, NewDealerRule>();
        services.AddScoped<IFraudRule, VoidPatternRule>();

        return services;
    }
}
