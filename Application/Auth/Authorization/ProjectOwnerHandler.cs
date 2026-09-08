using Application.Interfaces;
using Domain.Models;
using Microsoft.AspNetCore.Authorization;

namespace Application.Auth.Authorization;

public class ProjectOwnerHandler : AuthorizationHandler<ProjectOwnerRequirement, Project>
{
    private readonly ICurrentUser _currentUser;

    public ProjectOwnerHandler(ICurrentUser currentUser)
    {
        _currentUser = currentUser;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ProjectOwnerRequirement requirement,
        Project resource)
    {
        if (_currentUser.IsAdminOrManager || resource.OwnerId == _currentUser.UserId)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
