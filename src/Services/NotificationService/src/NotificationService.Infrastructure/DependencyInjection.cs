using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NotificationService.Application.Interfaces;
using NotificationService.Domain.Interfaces;
using NotificationService.Infrastructure.Messaging;
using NotificationService.Infrastructure.Persistence;
using NotificationService.Infrastructure.Repositories;
using NotificationService.Infrastructure.Services;

namespace NotificationService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<NotificationDbContext>(opt =>
            opt.UseSqlServer(config.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(NotificationDbContext).Assembly.FullName)));

        // Repositories
        services.AddScoped<INotificationLogRepository, NotificationLogRepository>();
        services.AddScoped<IPriceAlertSubscriptionRepository, PriceAlertSubscriptionRepository>();
        services.AddScoped<IProcessedEventRepository, ProcessedEventRepository>();

        // Services
        services.AddScoped<IEmailService, MailKitEmailService>();
        services.AddScoped<ISmsService, SmsService>();
        services.AddSingleton<IEmailTemplateService, EmailTemplateService>();

        // Messaging
        services.AddHostedService<NotificationConsumerHostedService>();

        return services;
    }
}
