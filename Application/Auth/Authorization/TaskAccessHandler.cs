using Application.Interfaces;
using Domain.Models;
using Domain.Repositories;
using Microsoft.AspNetCore.Authorization;

namespace Application.Auth.Authorization;

public class TaskAccessHandler : AuthorizationHandler<TaskAccessRequirement, WorkTask>
{
    private readonly ICurrentUser _currentUser;
    private readonly IProjectRepository _projectRepository;

    public TaskAccessHandler(ICurrentUser currentUser, IProjectRepository projectRepository)
    {
        _currentUser = currentUser;
        _projectRepository = projectRepository;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        TaskAccessRequirement requirement,
        WorkTask resource)
    {
        if (_currentUser.IsAdminOrManager
            || resource.Assignees.Any(assignee => assignee.Id == _currentUser.UserId))
        {
            context.Succeed(requirement);
            return;
        }

        var ownerId = resource.Project?.OwnerId
            ?? (await _projectRepository.GetProjectByIdAsync(resource.ProjectId))?.OwnerId;

        if (ownerId == _currentUser.UserId)
            context.Succeed(requirement);
    }
}
