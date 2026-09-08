using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.DTOs;

namespace Application.Interfaces;

    public interface IAuthService
    {
        Task<TokenResponse> RegisterAsync(RegisterRequest request);
        Task<TokenResponse> LoginAsync(LoginRequest request);
        Task<TokenResponse> RefreshTokenAsync(RefreshTokenRequest request);
        Task LogoutAsync(RefreshTokenRequest request);
    }
