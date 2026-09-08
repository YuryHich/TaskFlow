using Domain.Models;
using Domain.Repositories;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class EfRefreshTokenRepository : IRefreshTokenRepository
{
    private readonly AppDbContext _context;

    public EfRefreshTokenRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<RefreshToken?> GetRefreshTokenByHashAsync(string hash)
    {
        return await _context.RefreshTokens
            .Include(token => token.User)
            .FirstOrDefaultAsync(token => token.TokenHash == hash);
    }

    public async Task CreateRefreshTokenAsync(RefreshToken refreshToken)
    {
        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateRefreshTokenAsync(RefreshToken refreshToken)
    {
        await _context.SaveChangesAsync();
    }

    public async Task RevokeAllForUserAsync(Guid userId)
    {
        var tokens = await _context.RefreshTokens
            .Where(token => token.UserId == userId && token.RevokedAt == null)
            .ToListAsync();

        var revokedAt = DateTime.UtcNow;
        foreach (var token in tokens)
        {
            token.RevokedAt = revokedAt;
        }

        await _context.SaveChangesAsync();
    }
}
