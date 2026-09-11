using Application.Interfaces;
using Domain.Models;
using Domain.Repositories;
using Microsoft.AspNetCore.Authorization;

namespace Application.Auth.Authorization;

public class ProjectAccessHandler : AuthorizationHandler<ProjectAccessRequirement, Project>
{
    private readonly ICurrentUser _currentUser;
    private readonly IProjectRepository _projectRepository;

    public ProjectAccessHandler(ICurrentUser currentUser, IProjectRepository projectRepository)
    {
        _currentUser = currentUser;
        _projectRepository = projectRepository;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ProjectAccessRequirement requirement,
        Project resource)
    {
        if (_currentUser.IsAdminOrManager
            || await _projectRepository.UserHasProjectReadAccessAsync(resource.Id, _currentUser.UserId))
        {
            context.Succeed(requirement);
        }
    }
}
