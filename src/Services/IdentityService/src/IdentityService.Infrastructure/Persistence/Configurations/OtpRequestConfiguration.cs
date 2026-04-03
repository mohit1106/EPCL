using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using IdentityService.Domain.Entities;

namespace IdentityService.Infrastructure.Persistence.Configurations;

public class OtpRequestConfiguration : IEntityTypeConfiguration<OtpRequest>
{
    public void Configure(EntityTypeBuilder<OtpRequest> builder)
    {
        builder.ToTable("OtpRequests");

        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).HasDefaultValueSql("NEWID()");

        builder.Property(o => o.OtpCode).IsRequired().HasMaxLength(6).HasColumnType("VARCHAR(6)");
        builder.Property(o => o.Purpose)
            .IsRequired()
            .HasMaxLength(30)
            .HasColumnType("VARCHAR(30)")
            .HasConversion<string>();
        builder.Property(o => o.ExpiresAt).IsRequired();
        builder.Property(o => o.IsUsed).IsRequired().HasDefaultValue(false);
        builder.Property(o => o.CreatedAt).IsRequired().HasDefaultValueSql("GETUTCDATE()");
        builder.Property(o => o.IpAddress).HasMaxLength(45).HasColumnType("VARCHAR(45)");

        builder.HasIndex(o => o.UserId);
    }
}
