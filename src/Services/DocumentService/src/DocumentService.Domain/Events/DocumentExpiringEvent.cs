using System;

namespace DocumentService.Domain.Events
{
    public abstract record BaseEvent
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public DateTimeOffset OccurredOn { get; init; } = DateTimeOffset.UtcNow;
    }

    public record DocumentExpiringEvent : BaseEvent
    {
        public Guid DocumentId { get; init; }
        public string EntityType { get; init; } = string.Empty;
        public Guid EntityId { get; init; }
        public string DocumentType { get; init; } = string.Empty;
        public string FileName { get; init; } = string.Empty;
        public DateTime ExpiryDate { get; init; }
        public int DaysUntilExpiry { get; init; }
    }
}
