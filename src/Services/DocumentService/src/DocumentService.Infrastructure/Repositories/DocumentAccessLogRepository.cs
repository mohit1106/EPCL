using System.Threading;
using System.Threading.Tasks;
using DocumentService.Domain.Entities;
using DocumentService.Domain.Repositories;
using DocumentService.Infrastructure.Persistence;

namespace DocumentService.Infrastructure.Repositories
{
    public class DocumentAccessLogRepository : IDocumentAccessLogRepository
    {
        private readonly DocumentsDbContext _context;

        public DocumentAccessLogRepository(DocumentsDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(DocumentAccessLog log, CancellationToken ct = default)
        {
            await _context.DocumentAccessLogs.AddAsync(log, ct);
            await _context.SaveChangesAsync(ct);
        }
    }
}
