using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DocumentService.Domain.Interfaces;

namespace DocumentService.Infrastructure.Storage
{
    public class LocalFileStorageService : IFileStorageService
    {
        private readonly string _storagePath;

        public LocalFileStorageService()
        {
            _storagePath = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
            if (!Directory.Exists(_storagePath))
            {
                Directory.CreateDirectory(_storagePath);
            }
        }

        public async Task<string> SaveFileAsync(Stream fileStream, string fileName, CancellationToken ct = default)
        {
            var filePath = Path.Combine(_storagePath, fileName);
            using var fileStreamToWrite = new FileStream(filePath, FileMode.Create, FileAccess.Write);
            await fileStream.CopyToAsync(fileStreamToWrite, ct);
            return filePath;
        }

        public Task<Stream?> GetFileAsync(string filePath, CancellationToken ct = default)
        {
            if (!File.Exists(filePath))
            {
                return Task.FromResult<Stream?>(null);
            }

            return Task.FromResult<Stream?>(new FileStream(filePath, FileMode.Open, FileAccess.Read));
        }

        public Task DeleteFileAsync(string filePath, CancellationToken ct = default)
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            return Task.CompletedTask;
        }
    }
}
