using System;

namespace DocumentService.Domain.Entities
{
    public class DocumentAccessLog
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid DocumentId { get; set; }
        public Document Document { get; set; } = null!;
        public Guid AccessedByUserId { get; set; }
        public string AccessType { get; set; } = string.Empty; // View, Download
        public string? IpAddress { get; set; }
        public DateTime AccessedAt { get; set; } = DateTime.UtcNow;
    }
}
