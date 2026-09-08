using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.DTOs;
using Domain.Models;

namespace Application.Interfaces;

    public interface IJwtService
    {
       string GenerateAccessToken(User user, DateTime expiresAtUtc);
       DateTime GetAccessTokenExpiryUtc();
    }
