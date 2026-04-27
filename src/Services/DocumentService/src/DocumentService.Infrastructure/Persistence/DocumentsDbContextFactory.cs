using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DocumentService.Infrastructure.Persistence
{
    public class DocumentsDbContextFactory : IDesignTimeDbContextFactory<DocumentsDbContext>
    {
        public DocumentsDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<DocumentsDbContext>();
            optionsBuilder.UseSqlServer("Server=localhost\\SQLEXPRESS;Database=EPCL_Documents;Trusted_Connection=True;TrustServerCertificate=True;");

            return new DocumentsDbContext(optionsBuilder.Options);
        }
    }
}
