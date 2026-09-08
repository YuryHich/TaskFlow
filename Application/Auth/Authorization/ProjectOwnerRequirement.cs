using Microsoft.AspNetCore.Authorization;

namespace Application.Auth.Authorization;

public sealed class ProjectOwnerRequirement : IAuthorizationRequirement
{
}
