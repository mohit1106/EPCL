using Microsoft.EntityFrameworkCore;
using IdentityService.Domain.Entities;
using IdentityService.Domain.Interfaces;
using IdentityService.Infrastructure.Persistence;

namespace IdentityService.Infrastructure.Repositories;

public class ProcessedEventRepository(IdentityDbContext context) : IProcessedEventRepository
{
    public async Task<bool> AlreadyProcessedAsync(Guid eventId, CancellationToken ct = default)
    {
        return await context.ProcessedEvents.AnyAsync(e => e.EventId == eventId, ct);
    }

    public async Task MarkProcessedAsync(Guid eventId, string eventType, CancellationToken ct = default)
    {
        await context.ProcessedEvents.AddAsync(new ProcessedEvent
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            EventType = eventType,
            ProcessedAt = DateTimeOffset.UtcNow
        }, ct);
        await context.SaveChangesAsync(ct);
    }
}
