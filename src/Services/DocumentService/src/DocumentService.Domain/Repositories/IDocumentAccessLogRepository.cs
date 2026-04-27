using System.Threading;
using System.Threading.Tasks;
using DocumentService.Domain.Entities;

namespace DocumentService.Domain.Repositories
{
    public interface IDocumentAccessLogRepository
    {
        Task AddAsync(DocumentAccessLog log, CancellationToken ct = default);
    }
}
