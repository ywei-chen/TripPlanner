using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TripPlanner.Domain.Entities;
using TripPlanner.Domain.Enums;

namespace TripPlanner.Infrastructure.Persistence.Configurations;

public class TripConfiguration : IEntityTypeConfiguration<Trip>
{
    public void Configure(EntityTypeBuilder<Trip> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Title).HasMaxLength(255).IsRequired();
        builder.Property(t => t.CoverImage).HasMaxLength(1024);
        builder.Property(t => t.Status).HasConversion<string>();
        builder.HasIndex(t => t.UserId);

        builder.HasMany(t => t.Items).WithOne(i => i.Trip).HasForeignKey(i => i.TripId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(t => t.ShareLinks).WithOne(s => s.Trip).HasForeignKey(s => s.TripId).OnDelete(DeleteBehavior.Cascade);
    }
}
