using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Interfaces;
using Domain.Models;
using Domain.Repositories;

namespace Application.Services;

public class ProjectAudience : IProjectAudience
{
        private readonly IProjectRepository _projectRepository;
    private readonly IUserRepository _userRepository;

    public ProjectAudience(IProjectRepository projectRepository, IUserRepository userRepository)
    {
        _projectRepository = projectRepository;
        _userRepository = userRepository;
    }

    public async Task<IReadOnlyList<Guid>> GetUserIdsAsync(Guid projectId)
    {
        var project = await _projectRepository.GetProjectByIdAsync(projectId);
        if (project == null) return [];

        var staff = await _userRepository.GetUserIdsByRoleAsync(UserRole.Admin, UserRole.Manager);
        var tasks = await _projectRepository.GetProjectTasksAsync(projectId);

        return staff.Append(project.OwnerId)
        .Concat(tasks.SelectMany((task => task.Assignees.Select(assignee => assignee.Id))))
        .Distinct()
        .ToList();

    }
}

