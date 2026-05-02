using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using IdentityService.Domain.Entities;

namespace IdentityService.Infrastructure.Persistence.Configurations;

public class HelpRequestConfiguration : IEntityTypeConfiguration<HelpRequest>
{
    public void Configure(EntityTypeBuilder<HelpRequest> builder)
    {
        builder.ToTable("HelpRequests");
        builder.HasKey(h => h.Id);
        builder.Property(h => h.DealerEmail).HasMaxLength(256);
        builder.Property(h => h.DealerName).HasMaxLength(200);
        builder.Property(h => h.TargetAdminName).HasMaxLength(200);
        builder.Property(h => h.Category).HasMaxLength(100);
        builder.Property(h => h.Message).HasMaxLength(2000);
        builder.Property(h => h.Status).HasMaxLength(50);
        builder.HasIndex(h => h.DealerUserId);
        builder.HasIndex(h => h.TargetAdminId);
        builder.HasIndex(h => h.Status);
        builder.HasMany(h => h.Replies).WithOne(r => r.HelpRequest).HasForeignKey(r => r.HelpRequestId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class HelpRequestReplyConfiguration : IEntityTypeConfiguration<HelpRequestReply>
{
    public void Configure(EntityTypeBuilder<HelpRequestReply> builder)
    {
        builder.ToTable("HelpRequestReplies");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.FromRole).HasMaxLength(50);
        builder.Property(r => r.FromName).HasMaxLength(200);
        builder.Property(r => r.Message).HasMaxLength(2000);
    }
}
