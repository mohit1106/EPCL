using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using DocumentService.Domain.Repositories;
using DocumentService.Domain.Events;
using DocumentService.Infrastructure.Messaging;

namespace DocumentService.Infrastructure.BackgroundServices
{
    public class DocumentExpiryHostedService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DocumentExpiryHostedService> _logger;
        private readonly IDocumentRabbitMqPublisher _publisher;

        public DocumentExpiryHostedService(
            IServiceProvider serviceProvider,
            IDocumentRabbitMqPublisher publisher,
            ILogger<DocumentExpiryHostedService> logger)
        {
            _serviceProvider = serviceProvider;
            _publisher = publisher;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // Run at 8 AM IST every day
                var now = DateTimeOffset.UtcNow;
                var istNow = now.ToOffset(TimeSpan.FromHours(5.5));
                var nextRun = istNow.Date.AddDays(1).AddHours(8); // next 8 AM IST
                var delay = nextRun - istNow;

                if (delay > TimeSpan.Zero)
                {
                    await Task.Delay(delay, stoppingToken);
                }

                await CheckExpiringDocumentsAsync(stoppingToken);
            }
        }

        private async Task CheckExpiringDocumentsAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Checking for expiring documents...");

            using var scope = _serviceProvider.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IDocumentRepository>();

            // Find documents expiring in exactly 30 days
            var expiringDocs = await repository.GetExpiringDocumentsAsync(30, cancellationToken);
            var today = DateTime.UtcNow.Date;

            foreach (var doc in expiringDocs)
            {
                if (doc.ExpiryDate.HasValue)
                {
                    var daysUntil = (doc.ExpiryDate.Value.Date - today).Days;

                    // Only send alert if it's exactly 30, 15, 7, 3, or 1 days away to avoid spam
                    if (daysUntil is 30 or 15 or 7 or 3 or 1)
                    {
                        var expiryEvent = new DocumentExpiringEvent
                        {
                            DocumentId = doc.Id,
                            EntityType = doc.EntityType,
                            EntityId = doc.EntityId,
                            DocumentType = doc.DocumentType,
                            FileName = doc.FileName,
                            ExpiryDate = doc.ExpiryDate.Value,
                            DaysUntilExpiry = daysUntil
                        };

                        await _publisher.PublishAsync(expiryEvent, "document.expiring.alert", cancellationToken);
                        _logger.LogInformation("Published expiry event for document {DocId}. Days until expiry: {Days}", doc.Id, daysUntil);
                    }
                }
            }
        }
    }
}
