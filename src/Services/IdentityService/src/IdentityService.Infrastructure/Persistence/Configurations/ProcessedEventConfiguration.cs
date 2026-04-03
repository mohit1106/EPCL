using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using IdentityService.Domain.Entities;

namespace IdentityService.Infrastructure.Persistence.Configurations;

public class ProcessedEventConfiguration : IEntityTypeConfiguration<ProcessedEvent>
{
    public void Configure(EntityTypeBuilder<ProcessedEvent> builder)
    {
        builder.ToTable("ProcessedEvents");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.EventId).IsRequired();
        builder.Property(e => e.EventType).IsRequired().HasMaxLength(100);
        builder.Property(e => e.ProcessedAt).IsRequired().HasDefaultValueSql("GETUTCDATE()");

        builder.HasIndex(e => e.EventId).IsUnique();
    }
}
