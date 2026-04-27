using System;
using System.Collections.Generic;
using MediatR;
using DocumentService.Application.DTOs;

namespace DocumentService.Application.Queries
{
    public record GetDocumentsQuery(string? EntityType, Guid? EntityId, string? DocumentType) : IRequest<List<DocumentDto>>;

    public record GetExpiringDocumentsQuery(int DaysAhead = 30) : IRequest<List<DocumentDto>>;

    public record GetDocumentFileQuery(Guid Id, Guid AccessedByUserId, string? IpAddress, bool IsPreview = false) : IRequest<(System.IO.Stream Stream, string FileName, string MimeType)?>;
}
