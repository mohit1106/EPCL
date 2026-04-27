using System;
using MediatR;

namespace DocumentService.Application.Commands
{
    public record VerifyDocumentCommand(Guid Id, Guid VerifiedByUserId, string? Notes) : IRequest<bool>;

    public record SoftDeleteDocumentCommand(Guid Id) : IRequest<bool>;

    public record LogDocumentAccessCommand(Guid DocumentId, Guid AccessedByUserId, string AccessType, string? IpAddress) : IRequest;
}
