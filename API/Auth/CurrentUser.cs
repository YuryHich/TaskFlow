using Application.Interfaces;
using Domain.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace API.Auth;

public class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public ClaimsPrincipal User =>
        _httpContextAccessor.HttpContext?.User
        ?? new ClaimsPrincipal(new ClaimsIdentity());

    public Guid UserId
    {
        get
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

            return Guid.TryParse(value, out var userId)
                ? userId
                : Guid.Empty;
        }
    }

    public UserRole Role =>
        TryGetRole(out var role)
            ? role
            : throw new InvalidOperationException("Current user has no valid role claim.");

    public bool IsAdminOrManager =>
        TryGetRole(out var role) && role is UserRole.Admin or UserRole.Manager;

    private bool TryGetRole(out UserRole role)
    {
        var value = User.FindFirstValue(ClaimTypes.Role)
            ?? User.FindFirstValue("role");

        if (Enum.TryParse(value, ignoreCase: true, out role) && Enum.IsDefined(role))
            return true;

        role = default;
        return false;
    }
}
