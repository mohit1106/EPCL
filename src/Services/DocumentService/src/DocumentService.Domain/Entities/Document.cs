using System;

namespace DocumentService.Domain.Entities
{
    public class Document
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string EntityType { get; set; } = string.Empty; // Station, Tank, Compliance
        public Guid EntityId { get; set; }
        public string DocumentType { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string StoredFileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string MimeType { get; set; } = string.Empty;
        public DateTime? ExpiryDate { get; set; }
        public bool IsVerified { get; set; } = false;
        public Guid? VerifiedByUserId { get; set; }
        public DateTime? VerifiedAt { get; set; }
        public Guid UploadedByUserId { get; set; }
        public bool IsActive { get; set; } = true;
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public ICollection<DocumentAccessLog> AccessLogs { get; set; } = new List<DocumentAccessLog>();
    }
}
