using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DocumentService.Domain.Entities;

namespace DocumentService.Domain.Repositories
{
    public interface IDocumentRepository
    {
        Task<Document?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<List<Document>> GetDocumentsAsync(string? entityType, Guid? entityId, string? documentType, CancellationToken ct = default);
        Task<List<Document>> GetExpiringDocumentsAsync(int daysAhead, CancellationToken ct = default);
        Task AddAsync(Document document, CancellationToken ct = default);
        Task UpdateAsync(Document document, CancellationToken ct = default);
    }
}
