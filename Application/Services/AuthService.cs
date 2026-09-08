using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Auth;
using Application.Interfaces;
using Domain.Repositories;
using Domain.Models;
using Application.DTOs;
using Application.Exceptions;
using Mapster;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;


namespace Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IJwtService _jwtService;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly JwtOptions _jwtOptions;
        
        public AuthService(IUserRepository userRepository, IJwtService jwtService, IRefreshTokenRepository refreshTokenRepository, IPasswordHasher<User> passwordHasher, IOptions<JwtOptions> jwtOptions)
        {
            _userRepository = userRepository;
            _jwtService = jwtService;
            _refreshTokenRepository = refreshTokenRepository;
            _passwordHasher = passwordHasher;
            _jwtOptions = jwtOptions.Value;
        }

        public async Task<TokenResponse> RegisterAsync(RegisterRequest request)
        {
            if (await _userRepository.GetByUserNameAsync(request.Username) != null || await _userRepository.GetByEmailAsync(request.Email) != null)
            {
                throw new UserAlreadyExistsException("User with the specified username or email already exists.");
            }
            var newUser = request.Adapt<User>();
            newUser.Id = Guid.NewGuid();
            newUser.PasswordHash = _passwordHasher.HashPassword(newUser, request.Password);
            newUser.Role = UserRole.Developer;
            newUser.CreatedAt = DateTime.UtcNow;
            await _userRepository.CreateUserAsync(newUser);

            var accessTokenExpiration = _jwtService.GetAccessTokenExpiryUtc();
            var accessToken = _jwtService.GenerateAccessToken(newUser, accessTokenExpiration);
            var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
            var refreshTokenHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));
            var refreshTokenEntity = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = newUser.Id,
                TokenHash = refreshTokenHash,
                ExpiresAt = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenExpirationDays),
                CreatedAt = DateTime.UtcNow
            };
            await _refreshTokenRepository.CreateRefreshTokenAsync(refreshTokenEntity);
            return new TokenResponse
            {
                AccessToken = accessToken,
                ExpiresAt = accessTokenExpiration,
                RefreshToken = refreshToken
            };
        }

        public async Task<TokenResponse> LoginAsync(LoginRequest request)
        {
            var user = await _userRepository.GetByEmailAsync(request.Email);
            if (user == null || _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password) != PasswordVerificationResult.Success)
            {
                throw new UnauthorizedAccessException();
            }

            var accessTokenExpiration = _jwtService.GetAccessTokenExpiryUtc();
            var accessToken = _jwtService.GenerateAccessToken(user, accessTokenExpiration);
            var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
            var refreshTokenHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));
            var refreshTokenEntity = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                TokenHash = refreshTokenHash,
                ExpiresAt = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenExpirationDays),
                CreatedAt = DateTime.UtcNow
            };
            await _refreshTokenRepository.CreateRefreshTokenAsync(refreshTokenEntity);
            return new TokenResponse { AccessToken = accessToken,
                                      ExpiresAt = accessTokenExpiration,
                                      RefreshToken = refreshToken };
        }

        public async Task<TokenResponse> RefreshTokenAsync(RefreshTokenRequest request)
        {
           var refreshTokenHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(request.RefreshToken)));
            var refreshTokenEntity = await _refreshTokenRepository.GetRefreshTokenByHashAsync(refreshTokenHash);
            if (refreshTokenEntity == null)
            {
                throw new UnauthorizedAccessException();
            }
            if (refreshTokenEntity.RevokedAt != null)
            {
                await _refreshTokenRepository.RevokeAllForUserAsync(refreshTokenEntity.UserId);
                throw new UnauthorizedAccessException();
            }
            if (refreshTokenEntity.ExpiresAt < DateTime.UtcNow)
            {
                throw new UnauthorizedAccessException();
            }

            refreshTokenEntity.RevokedAt = DateTime.UtcNow;
            var user = refreshTokenEntity.User ?? await _userRepository.GetUserByIdAsync(refreshTokenEntity.UserId);
            if (user == null)
            {
                throw new UnauthorizedAccessException();
            }
            var accessTokenExpiration = _jwtService.GetAccessTokenExpiryUtc();
            var accessToken = _jwtService.GenerateAccessToken(user, accessTokenExpiration);
            var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
            var newRefreshTokenHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));
            var newRefreshTokenEntity = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                TokenHash = newRefreshTokenHash,
                ExpiresAt = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenExpirationDays),
                CreatedAt = DateTime.UtcNow
            };
            refreshTokenEntity.ReplacedByTokenHash = newRefreshTokenHash;
            await _refreshTokenRepository.CreateRefreshTokenAsync(newRefreshTokenEntity);
            await _refreshTokenRepository.UpdateRefreshTokenAsync(refreshTokenEntity);
            return new TokenResponse
            {
                AccessToken = accessToken,
                ExpiresAt = accessTokenExpiration,
                RefreshToken = refreshToken
            };
        }

        public async Task LogoutAsync(RefreshTokenRequest request)
        {
            var refreshTokenHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(request.RefreshToken)));
            var refreshTokenEntity = await _refreshTokenRepository.GetRefreshTokenByHashAsync(refreshTokenHash);
            if (refreshTokenEntity == null || refreshTokenEntity.ExpiresAt < DateTime.UtcNow || refreshTokenEntity.RevokedAt != null)
            {
               return;
            }
            refreshTokenEntity.RevokedAt = DateTime.UtcNow;
            await _refreshTokenRepository.UpdateRefreshTokenAsync(refreshTokenEntity);
        }
    }
}