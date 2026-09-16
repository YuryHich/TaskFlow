using System.Security.Claims;
using Domain.Models;
using Microsoft.AspNetCore.Authorization;

namespace Application.Auth.Authorization;

public static class AuthorizationServiceExtensions
{
    public static Task<AuthorizationResult> AuthorizeProjectOwnerAsync(
        this IAuthorizationService authorization,
        ClaimsPrincipal user,
        Project project)
        => authorization.AuthorizeAsync(user, project, AuthorizationPolicies.ProjectOwner);

    public static Task<AuthorizationResult> AuthorizeProjectAccessAsync(
        this IAuthorizationService authorization,
        ClaimsPrincipal user,
        Project project)
        => authorization.AuthorizeAsync(user, project, AuthorizationPolicies.ProjectAccess);

    public static Task<AuthorizationResult> AuthorizeTaskAccessAsync(
        this IAuthorizationService authorization,
        ClaimsPrincipal user,
        WorkTask task)
        => authorization.AuthorizeAsync(user, task, AuthorizationPolicies.TaskAccess);
}
