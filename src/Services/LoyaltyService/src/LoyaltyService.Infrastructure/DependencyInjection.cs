using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using LoyaltyService.Domain.Interfaces;
using LoyaltyService.Infrastructure.Messaging;
using LoyaltyService.Infrastructure.Persistence;
using LoyaltyService.Infrastructure.Repositories;

namespace LoyaltyService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<LoyaltyDbContext>(opt =>
            opt.UseSqlServer(config.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(LoyaltyDbContext).Assembly.FullName)));

        services.AddScoped<ILoyaltyAccountRepository, LoyaltyAccountRepository>();
        services.AddScoped<ILoyaltyTransactionRepository, LoyaltyTransactionRepository>();
        services.AddScoped<IReferralCodeRepository, ReferralCodeRepository>();
        services.AddScoped<IReferralRedemptionRepository, ReferralRedemptionRepository>();
        services.AddScoped<IProcessedEventRepository, ProcessedEventRepository>();

        services.AddHostedService<LoyaltyConsumerHostedService>();

        return services;
    }
}
