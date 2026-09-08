using Domain.Models;

namespace Domain.Repositories;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetRefreshTokenByHashAsync(string hash);
    Task CreateRefreshTokenAsync(RefreshToken refreshToken);
    Task UpdateRefreshTokenAsync(RefreshToken refreshToken);
    Task RevokeAllForUserAsync(Guid userId);
}
