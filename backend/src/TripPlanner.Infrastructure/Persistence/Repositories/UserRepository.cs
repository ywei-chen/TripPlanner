using Microsoft.EntityFrameworkCore;
using TripPlanner.Application.Auth.Handlers;
using TripPlanner.Domain.Entities;

namespace TripPlanner.Infrastructure.Persistence.Repositories;

public class UserRepository(AppDbContext db) : IUserRepository
{
    public Task<User?> GetByEmailAsync(string email) =>
        db.Users.FirstOrDefaultAsync(u => u.Email == email);

    public Task<User?> GetByIdAsync(Guid id) =>
        db.Users.FindAsync(id).AsTask();

    public Task<bool> ExistsByEmailAsync(string email) =>
        db.Users.AnyAsync(u => u.Email == email);

    public Task<bool> ExistsByUsernameAsync(string username) =>
        db.Users.AnyAsync(u => u.Username == username);

    public async Task AddAsync(User user) => await db.Users.AddAsync(user);

    public Task<RefreshToken?> GetRefreshTokenAsync(string token) =>
        db.RefreshTokens.Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Token == token);

    public async Task AddRefreshTokenAsync(RefreshToken token) =>
        await db.RefreshTokens.AddAsync(token);

    public async Task RevokeRefreshTokenAsync(string token)
    {
        var t = await db.RefreshTokens.FirstOrDefaultAsync(r => r.Token == token);
        if (t is not null) t.RevokedAt = DateTime.UtcNow;
    }

    public Task SaveChangesAsync() => db.SaveChangesAsync();
}
