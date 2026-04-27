using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DocumentService.Domain.Entities;
using DocumentService.Domain.Repositories;
using DocumentService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DocumentService.Infrastructure.Repositories
{
    public class DocumentRepository : IDocumentRepository
    {
        private readonly DocumentsDbContext _context;

        public DocumentRepository(DocumentsDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Document document, CancellationToken ct = default)
        {
            await _context.Documents.AddAsync(document, ct);
            await _context.SaveChangesAsync(ct);
        }

        public async Task<Document?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            return await _context.Documents.FirstOrDefaultAsync(d => d.Id == id && d.IsActive, ct);
        }

        public async Task<List<Document>> GetDocumentsAsync(string? entityType, Guid? entityId, string? documentType, CancellationToken ct = default)
        {
            var query = _context.Documents.Where(d => d.IsActive).AsQueryable();

            if (!string.IsNullOrEmpty(entityType)) query = query.Where(d => d.EntityType == entityType);
            if (entityId.HasValue) query = query.Where(d => d.EntityId == entityId.Value);
            if (!string.IsNullOrEmpty(documentType)) query = query.Where(d => d.DocumentType == documentType);

            return await query.OrderByDescending(d => d.CreatedAt).ToListAsync(ct);
        }

        public async Task<List<Document>> GetExpiringDocumentsAsync(int daysAhead, CancellationToken ct = default)
        {
            var targetDate = DateTime.UtcNow.AddDays(daysAhead).Date;
            var today = DateTime.UtcNow.Date;

            return await _context.Documents
                .Where(d => d.IsActive && d.ExpiryDate.HasValue && 
                            d.ExpiryDate.Value.Date >= today && 
                            d.ExpiryDate.Value.Date <= targetDate)
                .OrderBy(d => d.ExpiryDate)
                .ToListAsync(ct);
        }

        public async Task UpdateAsync(Document document, CancellationToken ct = default)
        {
            _context.Documents.Update(document);
            await _context.SaveChangesAsync(ct);
        }
    }
}
