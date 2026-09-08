using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Domain.Models;

namespace Application.Interfaces
{
    public interface ICurrentUser
    {
        ClaimsPrincipal User { get; }
        Guid UserId { get; }
        UserRole Role { get; }
        bool IsAdminOrManager { get; }
    }
}