using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using IdentityService.Domain.Entities;

namespace IdentityService.Infrastructure.Persistence.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasDefaultValueSql("NEWID()");

        builder.Property(t => t.Token).IsRequired().HasMaxLength(500);
        builder.Property(t => t.ExpiresAt).IsRequired();
        builder.Property(t => t.IsRevoked).IsRequired().HasDefaultValue(false);
        builder.Property(t => t.CreatedAt).IsRequired().HasDefaultValueSql("GETUTCDATE()");
        builder.Property(t => t.RevokedAt);
        builder.Property(t => t.ReplacedByToken).HasMaxLength(500);

        builder.HasIndex(t => t.Token).IsUnique();
        builder.HasIndex(t => t.UserId);
    }
}
