using System;

namespace DocumentService.Application.DTOs
{
    public class DocumentDto
    {
        public Guid Id { get; set; }
        public string EntityType { get; set; } = string.Empty;
        public Guid EntityId { get; set; }
        public string DocumentType { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string MimeType { get; set; } = string.Empty;
        public DateTime? ExpiryDate { get; set; }
        public bool IsVerified { get; set; }
        public Guid? VerifiedByUserId { get; set; }
        public DateTime? VerifiedAt { get; set; }
        public Guid UploadedByUserId { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
