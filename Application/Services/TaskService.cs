using Application.Auth.Authorization;
using Application.DTOs;
using Application.Interfaces;
using Domain.Models;
using Domain.Repositories;
using Mapster;
using Microsoft.AspNetCore.Authorization;

namespace Application.Services;

public class TaskService : ITaskService
{
    private readonly ITaskRepository _taskRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUser _currentUser;
    private readonly IAuthorizationService _authorizationService;

    public TaskService(
        ITaskRepository taskRepository,
        IProjectRepository projectRepository,
        IUserRepository userRepository,
        ICurrentUser currentUser,
        IAuthorizationService authorizationService)
    {
        _taskRepository = taskRepository;
        _projectRepository = projectRepository;
        _userRepository = userRepository;
        _currentUser = currentUser;
        _authorizationService = authorizationService;
    }

    public async Task<IEnumerable<TaskDto>> GetTasksAsync()
    {
        var tasks = await _taskRepository.GetTasksAsync();
        if (_currentUser.IsAdminOrManager)
        {
            return tasks.Adapt<IEnumerable<TaskDto>>();
        }
        else
        {
            var projects = await _projectRepository.GetProjectsAsync();
            var ownedProjectIds = projects
                .Where(p => p.OwnerId == _currentUser.UserId)
                .Select(p => p.Id)
                .ToHashSet();

            return tasks
                .Where(t => t.AssigneeId == _currentUser.UserId || ownedProjectIds.Contains(t.ProjectId))
                .Adapt<IEnumerable<TaskDto>>();
        }
    }

    public async Task<TaskDto?> GetTaskByIdAsync(Guid id)
    {
        var task = await _taskRepository.GetTaskByIdAsync(id);
        if (task is null) throw new NotFoundException("Task not found");
        var authorizationResult = await _authorizationService.AuthorizeTaskAccessAsync(_currentUser.User, task);
        if (!authorizationResult.Succeeded)
        throw new ForbiddenException("You are not authorized to access this task");
        return task.Adapt<TaskDto>();
    }

    public async Task<IEnumerable<CommentDto>> GetTaskCommentsAsync(Guid taskId)
    {
        var task = await _taskRepository.GetTaskByIdAsync(taskId);
         if (task is null) throw new NotFoundException("Task not found");
         var authorizationResult = await _authorizationService.AuthorizeTaskAccessAsync(_currentUser.User, task);
        if (!authorizationResult.Succeeded) throw new ForbiddenException("You are not authorized to access this task");
        var comments = await _taskRepository.GetTaskCommentsAsync(taskId);
        return comments.Adapt<IEnumerable<CommentDto>>();
    }

    public async Task<TaskDto> CreateTaskAsync(CreateTaskDto task)
    {
        var project = await _projectRepository.GetProjectByIdAsync(task.ProjectId);
        if (project is null) throw new NotFoundException("Project not found");
        var authorizationResult = await _authorizationService.AuthorizeProjectOwnerAsync(_currentUser.User, project);
        if (!authorizationResult.Succeeded) throw new ForbiddenException("You are not authorized to create a task in this project");

        if (task.AssigneeId.HasValue)
            await EnsureUserExistsAsync(task.AssigneeId.Value);

        var taskEntity = task.Adapt<WorkTask>();
        taskEntity.Id = Guid.NewGuid();
        taskEntity.CreatedAt = DateTime.UtcNow;

        await _taskRepository.CreateTaskAsync(taskEntity);
        return taskEntity.Adapt<TaskDto>();
    }

    public async Task UpdateTaskAsync(Guid id, UpdateTaskDto task)
    {
        var taskEntity = await _taskRepository.GetTaskByIdAsync(id);
        if (taskEntity is null)
        {
            throw new NotFoundException("Task not found");
        }

        var project = await _projectRepository.GetProjectByIdAsync(taskEntity.ProjectId);
        if (project is null) throw new NotFoundException("Project not found");
        var authorizationResult = await _authorizationService.AuthorizeProjectOwnerAsync(_currentUser.User, project);
        if (!authorizationResult.Succeeded) throw new ForbiddenException("You are not authorized to update this task");

        if (task.AssigneeId.HasValue)
            await EnsureUserExistsAsync(task.AssigneeId.Value);

        task.Adapt(taskEntity);
        await _taskRepository.UpdateTaskAsync(taskEntity);
    }

    public async Task DeleteTaskAsync(Guid id)
    {
        var taskEntity = await _taskRepository.GetTaskByIdAsync(id);
        if (taskEntity is null) throw new NotFoundException("Task not found");
        var project = await _projectRepository.GetProjectByIdAsync(taskEntity.ProjectId);
        if (project is null) throw new NotFoundException("Project not found");
        var authorizationResult = await _authorizationService.AuthorizeProjectOwnerAsync(_currentUser.User, project);
        if (!authorizationResult.Succeeded) throw new ForbiddenException("You are not authorized to delete this task");
        await _taskRepository.DeleteTaskAsync(id);
    }

    private async Task EnsureUserExistsAsync(Guid userId)
    {
        var user = await _userRepository.GetUserByIdAsync(userId);
        if (user is null) throw new NotFoundException("User not found");
    }
}
