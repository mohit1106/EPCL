using DocumentService.Domain.Interfaces;
using DocumentService.Domain.Repositories;
using DocumentService.Infrastructure.Persistence;
using DocumentService.Infrastructure.Repositories;
using DocumentService.Infrastructure.Storage;
using DocumentService.Infrastructure.BackgroundServices;
using DocumentService.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DocumentService.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<DocumentsDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

            services.AddScoped<IDocumentRepository, DocumentRepository>();
            services.AddScoped<IDocumentAccessLogRepository, DocumentAccessLogRepository>();
            services.AddSingleton<IFileStorageService, LocalFileStorageService>();

            // Raw RabbitMQ publisher (no MassTransit license required)
            services.AddSingleton<IDocumentRabbitMqPublisher, DocumentRabbitMqPublisher>();

            services.AddHostedService<DocumentExpiryHostedService>();

            return services;
        }
    }
}
