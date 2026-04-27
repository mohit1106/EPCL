using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using DocumentService.Application.DTOs;
using DocumentService.Domain.Entities;
using DocumentService.Domain.Interfaces;
using DocumentService.Domain.Repositories;

namespace DocumentService.Application.Commands
{
    public class UploadDocumentCommandHandler : IRequestHandler<UploadDocumentCommand, DocumentDto>
    {
        private readonly IDocumentRepository _repository;
        private readonly IFileStorageService _storageService;

        public UploadDocumentCommandHandler(IDocumentRepository repository, IFileStorageService storageService)
        {
            _repository = repository;
            _storageService = storageService;
        }

        public async Task<DocumentDto> Handle(UploadDocumentCommand request, CancellationToken cancellationToken)
        {
            var extension = Path.GetExtension(request.FileName);
            var storedFileName = $"{Guid.NewGuid()}{extension}";

            var filePath = await _storageService.SaveFileAsync(request.FileStream, storedFileName, cancellationToken);

            var document = new Document
            {
                EntityType = request.EntityType,
                EntityId = request.EntityId,
                DocumentType = request.DocumentType,
                FileName = request.FileName,
                StoredFileName = storedFileName,
                FilePath = filePath,
                FileSize = request.FileSize,
                MimeType = request.MimeType,
                ExpiryDate = request.ExpiryDate?.Date,
                UploadedByUserId = request.UploadedByUserId,
                Notes = request.Notes
            };

            await _repository.AddAsync(document, cancellationToken);

            return new DocumentDto
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
}
