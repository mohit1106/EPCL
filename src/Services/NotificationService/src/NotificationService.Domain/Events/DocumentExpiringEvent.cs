using System;

namespace NotificationService.Domain.Events
{
    public class DocumentExpiringEvent
    {
        public Guid DocumentId { get; set; }
        public string EntityType { get; set; } = string.Empty;
        public Guid EntityId { get; set; }
        public string DocumentType { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public DateTime ExpiryDate { get; set; }
        public int DaysUntilExpiry { get; set; }
    }
}
