using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using DocumentService.Application.DTOs;
using DocumentService.Domain.Repositories;
using DocumentService.Domain.Interfaces;
using DocumentService.Domain.Entities;

namespace DocumentService.Application.Queries
{
    public class DocumentQueryHandlers : 
        IRequestHandler<GetDocumentsQuery, List<DocumentDto>>,
        IRequestHandler<GetExpiringDocumentsQuery, List<DocumentDto>>,
        IRequestHandler<GetDocumentFileQuery, (System.IO.Stream Stream, string FileName, string MimeType)?>
    {
        private readonly IDocumentRepository _repository;
        private readonly IDocumentAccessLogRepository _logRepository;
        private readonly IFileStorageService _storageService;

        public DocumentQueryHandlers(IDocumentRepository repository, IDocumentAccessLogRepository logRepository, IFileStorageService storageService)
        {
            _repository = repository;
            _logRepository = logRepository;
            _storageService = storageService;
        }

        public async Task<List<DocumentDto>> Handle(GetDocumentsQuery request, CancellationToken cancellationToken)
        {
            var documents = await _repository.GetDocumentsAsync(request.EntityType, request.EntityId, request.DocumentType, cancellationToken);
            return documents.Select(MapToDto).ToList();
        }

        public async Task<List<DocumentDto>> Handle(GetExpiringDocumentsQuery request, CancellationToken cancellationToken)
        {
            var documents = await _repository.GetExpiringDocumentsAsync(request.DaysAhead, cancellationToken);
            return documents.Select(MapToDto).ToList();
        }

        public async Task<(System.IO.Stream Stream, string FileName, string MimeType)?> Handle(GetDocumentFileQuery request, CancellationToken cancellationToken)
        {
            var document = await _repository.GetByIdAsync(request.Id, cancellationToken);
            if (document == null) return null;

            var stream = await _storageService.GetFileAsync(document.FilePath, cancellationToken);
            if (stream == null) return null;

            // Log access
            var log = new DocumentAccessLog
            {
                DocumentId = document.Id,
                AccessedByUserId = request.AccessedByUserId,
                AccessType = request.IsPreview ? "Preview" : "Download",
                IpAddress = request.IpAddress
            };
            await _logRepository.AddAsync(log, cancellationToken);

            return (stream, document.FileName, document.MimeType);
        }

        private static DocumentDto MapToDto(Document document) => new()
        {
            Id = document.Id,
            EntityType = document.EntityType,
            EntityId = document.EntityId,
            DocumentType = document.DocumentType,
            FileName = document.FileName,
            FileSize = document.FileSize,
            MimeType = document.MimeType,
            ExpiryDate = document.ExpiryDate,
            IsVerified = document.IsVerified,
            VerifiedByUserId = document.VerifiedByUserId,
            VerifiedAt = document.VerifiedAt,
            UploadedByUserId = document.UploadedByUserId,
            Notes = document.Notes,
            CreatedAt = document.CreatedAt
        };
    }
}
