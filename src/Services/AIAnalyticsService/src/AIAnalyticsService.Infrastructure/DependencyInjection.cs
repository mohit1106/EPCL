using AIAnalyticsService.Domain.Interfaces;
using AIAnalyticsService.Infrastructure.Persistence;
using AIAnalyticsService.Infrastructure.Repositories;
using AIAnalyticsService.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AIAnalyticsService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services,
        IConfiguration configuration)
    {
        // EF Core DbContext for ConversationHistory and QueryLog tables
        services.AddDbContext<AnalyticsDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(AnalyticsDbContext).Assembly.FullName)));

        // Repositories
        services.AddScoped<IConversationRepository, ConversationRepository>();
        services.AddScoped<IQueryLogRepository, QueryLogRepository>();

        // Gemini AI service
        services.Configure<GeminiSettings>(configuration.GetSection("Gemini"));
        services.AddHttpClient<IGeminiService, GeminiService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // Analytics query service (ADO.NET, uses AnalyticsConnection string)
        services.AddSingleton<IAnalyticsQueryService, AnalyticsQueryService>();

        return services;
    }
}
