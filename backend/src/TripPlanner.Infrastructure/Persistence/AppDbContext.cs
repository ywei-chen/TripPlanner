using Microsoft.EntityFrameworkCore;
using TripPlanner.Domain.Entities;

namespace TripPlanner.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Attraction> Attractions => Set<Attraction>();
    public DbSet<Trip> Trips => Set<Trip>();
    public DbSet<TripItem> TripItems => Set<TripItem>();
    public DbSet<Favorite> Favorites => Set<Favorite>();
    public DbSet<ShareLink> ShareLinks => Set<ShareLink>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
