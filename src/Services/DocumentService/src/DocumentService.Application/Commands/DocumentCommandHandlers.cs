using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using DocumentService.Domain.Entities;
using DocumentService.Domain.Repositories;

namespace DocumentService.Application.Commands
{
    public class DocumentCommandHandlers : 
        IRequestHandler<VerifyDocumentCommand, bool>,
        IRequestHandler<SoftDeleteDocumentCommand, bool>,
        IRequestHandler<LogDocumentAccessCommand>
    {
        private readonly IDocumentRepository _repository;
        private readonly IDocumentAccessLogRepository _logRepository;

        public DocumentCommandHandlers(IDocumentRepository repository, IDocumentAccessLogRepository logRepository)
        {
            _repository = repository;
            _logRepository = logRepository;
        }

        public async Task<bool> Handle(VerifyDocumentCommand request, CancellationToken cancellationToken)
        {
            var document = await _repository.GetByIdAsync(request.Id, cancellationToken);
            if (document == null) return false;

            document.IsVerified = true;
            document.VerifiedByUserId = request.VerifiedByUserId;
            document.VerifiedAt = DateTime.UtcNow;
            if (!string.IsNullOrEmpty(request.Notes))
            {
                document.Notes = string.IsNullOrEmpty(document.Notes) 
                    ? request.Notes 
                    : $"{document.Notes}\nVerify Notes: {request.Notes}";
            }

            await _repository.UpdateAsync(document, cancellationToken);
            return true;
        }

        public async Task<bool> Handle(SoftDeleteDocumentCommand request, CancellationToken cancellationToken)
        {
            var document = await _repository.GetByIdAsync(request.Id, cancellationToken);
            if (document == null) return false;

            document.IsActive = false;
            await _repository.UpdateAsync(document, cancellationToken);
            return true;
        }

        public async Task Handle(LogDocumentAccessCommand request, CancellationToken cancellationToken)
        {
            var log = new DocumentAccessLog
            {
                DocumentId = request.DocumentId,
                AccessedByUserId = request.AccessedByUserId,
                AccessType = request.AccessType,
                IpAddress = request.IpAddress
            };
            await _logRepository.AddAsync(log, cancellationToken);
        }
    }
}
