using System;
using System.IO;
using MediatR;
using DocumentService.Application.DTOs;

namespace DocumentService.Application.Commands
{
    public record UploadDocumentCommand(
        Stream FileStream,
        string FileName,
        long FileSize,
        string MimeType,
        string EntityType,
        Guid EntityId,
        string DocumentType,
        DateTime? ExpiryDate,
        string? Notes,
        Guid UploadedByUserId
    ) : IRequest<DocumentDto>;
}
