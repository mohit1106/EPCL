using DocumentService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DocumentService.Infrastructure.Persistence
{
    public class DocumentsDbContext : DbContext
    {
        public DocumentsDbContext(DbContextOptions<DocumentsDbContext> options) : base(options) { }

        public DbSet<Document> Documents { get; set; } = null!;
        public DbSet<DocumentAccessLog> DocumentAccessLogs { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Document>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.EntityType).HasMaxLength(30).IsRequired();
                entity.Property(e => e.DocumentType).HasMaxLength(50).IsRequired();
                entity.Property(e => e.FileName).HasMaxLength(255).IsRequired();
                entity.Property(e => e.StoredFileName).HasMaxLength(255).IsRequired();
                entity.Property(e => e.FilePath).HasMaxLength(500).IsRequired();
                entity.Property(e => e.MimeType).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Notes).HasMaxLength(500);

                entity.HasIndex(e => e.EntityId);
                entity.HasIndex(e => e.ExpiryDate);
                entity.HasIndex(e => new { e.EntityType, e.EntityId });
            });

            modelBuilder.Entity<DocumentAccessLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.AccessType).HasMaxLength(20).IsRequired();
                entity.Property(e => e.IpAddress).HasMaxLength(45);

                entity.HasOne(d => d.Document)
                    .WithMany(p => p.AccessLogs)
                    .HasForeignKey(d => d.DocumentId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
