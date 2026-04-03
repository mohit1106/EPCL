using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using IdentityService.Domain.Entities;

namespace IdentityService.Infrastructure.Persistence.Configurations;

public class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.ToTable("UserProfiles");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasDefaultValueSql("NEWID()");

        builder.Property(p => p.AadhaarLast4).HasMaxLength(4).HasColumnType("VARCHAR(4)");
        builder.Property(p => p.AddressLine1).HasMaxLength(250);
        builder.Property(p => p.City).HasMaxLength(100);
        builder.Property(p => p.State).HasMaxLength(100);
        builder.Property(p => p.PinCode).HasMaxLength(6).HasColumnType("VARCHAR(6)");
        builder.Property(p => p.StationId);
        builder.Property(p => p.PreferredLanguage)
            .IsRequired()
            .HasMaxLength(5)
            .HasColumnType("VARCHAR(5)")
            .HasDefaultValue("en");

        builder.HasIndex(p => p.UserId).IsUnique();
    }
}
