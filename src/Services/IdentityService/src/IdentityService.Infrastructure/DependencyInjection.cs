using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using IdentityService.Application.Interfaces;
using IdentityService.Domain.Interfaces;
using IdentityService.Infrastructure.Email;
using IdentityService.Infrastructure.Messaging;
using IdentityService.Infrastructure.Persistence;
using IdentityService.Infrastructure.Repositories;
using IdentityService.Infrastructure.Services;

namespace IdentityService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        services.AddDbContext<IdentityDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(IdentityDbContext).Assembly.FullName)));

        // Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IOtpRepository, OtpRepository>();
        services.AddScoped<IProcessedEventRepository, ProcessedEventRepository>();

        // JWT
        services.AddScoped<IJwtService, JwtService>();

        // Google OAuth
        services.AddScoped<IGoogleAuthService, GoogleAuthService>();

        // Email
        services.Configure<EmailSettings>(options =>
        {
            options.Host = configuration["SMTP_HOST"] ?? "smtp.gmail.com";
            options.Port = int.Parse(configuration["SMTP_PORT"] ?? "587");
            options.Username = configuration["SMTP_USER"] ?? string.Empty;
            options.Password = configuration["SMTP_PASS"] ?? string.Empty;
            options.FromName = configuration["SMTP_FROM_NAME"] ?? "EPCL";
            options.FromAddress = configuration["SMTP_FROM_ADDRESS"] ?? string.Empty;
        });
        services.AddScoped<IEmailService, GmailSmtpEmailService>();
        services.AddScoped<IEmailTemplateService, EmailTemplateService>();

        // RabbitMQ
        services.AddSingleton<IRabbitMqPublisher, RabbitMqPublisher>();

        return services;
    }
}
