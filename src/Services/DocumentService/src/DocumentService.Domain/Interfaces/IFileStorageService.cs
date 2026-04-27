using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DocumentService.Domain.Interfaces
{
    public interface IFileStorageService
    {
        Task<string> SaveFileAsync(Stream fileStream, string fileName, CancellationToken ct = default);
        Task<Stream?> GetFileAsync(string filePath, CancellationToken ct = default);
        Task DeleteFileAsync(string filePath, CancellationToken ct = default);
    }
}
