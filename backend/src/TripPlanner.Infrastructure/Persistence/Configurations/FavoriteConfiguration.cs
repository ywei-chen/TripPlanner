using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TripPlanner.Domain.Entities;

namespace TripPlanner.Infrastructure.Persistence.Configurations;

public class FavoriteConfiguration : IEntityTypeConfiguration<Favorite>
{
    public void Configure(EntityTypeBuilder<Favorite> builder)
    {
        builder.HasKey(f => new { f.UserId, f.AttractionId });
        builder.HasOne(f => f.Attraction).WithMany(a => a.Favorites)
            .HasForeignKey(f => f.AttractionId).OnDelete(DeleteBehavior.Cascade);
    }
}
