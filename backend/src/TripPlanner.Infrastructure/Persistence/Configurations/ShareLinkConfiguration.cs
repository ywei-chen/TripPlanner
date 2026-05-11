using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TripPlanner.Domain.Entities;

namespace TripPlanner.Infrastructure.Persistence.Configurations;

public class ShareLinkConfiguration : IEntityTypeConfiguration<ShareLink>
{
    public void Configure(EntityTypeBuilder<ShareLink> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.ShareToken).HasMaxLength(64).IsRequired();
        builder.Property(s => s.Permission).HasConversion<string>();
        builder.HasIndex(s => s.ShareToken).IsUnique();
        builder.HasIndex(s => s.TripId);
    }
}
