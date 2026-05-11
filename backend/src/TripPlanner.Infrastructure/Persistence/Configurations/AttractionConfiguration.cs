using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TripPlanner.Domain.Entities;

namespace TripPlanner.Infrastructure.Persistence.Configurations;

public class AttractionConfiguration : IEntityTypeConfiguration<Attraction>
{
    public void Configure(EntityTypeBuilder<Attraction> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Name).HasMaxLength(255).IsRequired();
        builder.Property(a => a.Category).HasMaxLength(100);
        builder.Property(a => a.City).HasMaxLength(100);
        builder.Property(a => a.Country).HasMaxLength(100);
        builder.Property(a => a.CoverImage).HasMaxLength(1024);
        builder.Property(a => a.Rating).HasPrecision(3, 1);
        builder.Property(a => a.Latitude).HasPrecision(9, 6);
        builder.Property(a => a.Longitude).HasPrecision(9, 6);
        builder.Property(a => a.Tags).HasColumnType("text[]");
        builder.HasIndex(a => a.Category);
        builder.HasIndex(a => a.City);
    }
}
