using Application.Auth.Authorization;
using Application.Caching;
using Application.DTOs;
using Application.Events;
using Application.Exceptions;
using Application.Interfaces;
using Domain.Models;
using Domain.Repositories;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Application.Services;

public class TaskService : ITaskService
{
    private readonly ITaskRepository _taskRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUser _currentUser;
    private readonly IAuthorizationService _authorizationService;
    private readonly IAppEventPublisher _appEventPublisher;
    private readonly ICacheService _cache;
    private readonly IOptions<CacheOptions> _cacheOptions;
    public TaskService(
        ITaskRepository taskRepository,
        IProjectRepository projectRepository,
        IUserRepository userRepository,
        ICurrentUser currentUser,
        IAuthorizationService authorizationService,
        IAppEventPublisher appEventPublisher,
        ICacheService cache,
        IOptions<CacheOptions> cacheOptions)
    {
        _taskRepository = taskRepository;
        _projectRepository = projectRepository;
        _userRepository = userRepository;
        _currentUser = currentUser;
        _authorizationService = authorizationService;
        _appEventPublisher = appEventPublisher;
        _cache = cache;
        _cacheOptions = cacheOptions;
    }

    public async Task<IEnumerable<TaskDto>> GetTasksAsync()
    {
        var filter = _currentUser.IsAdminOrManager ? (Guid?)null : _currentUser.UserId;
        var tasks = await _taskRepository.GetTasksAsync(filter);
        return tasks.Adapt<IEnumerable<TaskDto>>();
    }

    public async Task<TaskDto?> GetTaskByIdAsync(Guid id)
    {
        var cached = await _cache.GetAsync<TaskDto>(CacheKeys.Task(id));
        if (cached is not null)
        {
            var cachedTask = new WorkTask { Id = cached.Id, ProjectId = cached.ProjectId };
            var cachedAuth = await _authorizationService.AuthorizeTaskAccessAsync(_currentUser.User, cachedTask);
            if (!cachedAuth.Succeeded)
                throw new ForbiddenException("You are not authorized to access this task");
            return cached;
        }
        var task = await _taskRepository.GetTaskByIdAsync(id);
        if (task is null)
            throw new NotFoundException("Task not found");
        var authorizationResult = await _authorizationService.AuthorizeTaskAccessAsync(_currentUser.User, task);
        if (!authorizationResult.Succeeded)
            throw new ForbiddenException("You are not authorized to access this task");
        var dto = task.Adapt<TaskDto>();
        await _cache.SetAsync(
            CacheKeys.Task(id),
            dto,
            TimeSpan.FromMinutes(_cacheOptions.Value.TaskMinutes));
        return dto;
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

        var assignees = await LoadAssigneesAsync(task.AssigneeIds);

        var taskEntity = task.Adapt<WorkTask>();
        taskEntity.Id = Guid.NewGuid();
        taskEntity.CreatedAt = DateTime.UtcNow;
        taskEntity.Assignees = assignees;

        await _taskRepository.CreateTaskAsync(taskEntity);
        await _appEventPublisher.PublishAsync(new TaskCreatedEvent(taskEntity.Id, taskEntity.ProjectId));
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

        var assignees = await LoadAssigneesAsync(task.AssigneeIds);

        task.Adapt(taskEntity);
        taskEntity.Assignees.Clear();
        foreach (var assignee in assignees)
            taskEntity.Assignees.Add(assignee);

        await _taskRepository.UpdateTaskAsync(taskEntity);
        await _appEventPublisher.PublishAsync(new TaskUpdatedEvent(taskEntity.Id, taskEntity.ProjectId));
    }

    public async Task DeleteTaskAsync(Guid id)
    {
        var taskEntity = await _taskRepository.GetTaskByIdAsync(id);
        if (taskEntity is null) throw new NotFoundException("Task not found");
        var project = await _projectRepository.GetProjectByIdAsync(taskEntity.ProjectId);
        if (project is null) throw new NotFoundException("Project not found");
        var authorizationResult = await _authorizationService.AuthorizeProjectOwnerAsync(_currentUser.User, project);
        if (!authorizationResult.Succeeded) throw new ForbiddenException("You are not authorized to delete this task");
        var taskId = taskEntity.Id;
        var projectId = taskEntity.ProjectId;
        await _taskRepository.DeleteTaskAsync(id);
        await _appEventPublisher.PublishAsync(new TaskDeletedEvent(taskId, projectId));
    }

    private async Task<List<User>> LoadAssigneesAsync(IReadOnlyList<Guid> assigneeIds)
    {
        var users = new List<User>(assigneeIds.Count);
        foreach (var assigneeId in assigneeIds.Distinct())
        {
            var user = await _userRepository.GetUserByIdAsync(assigneeId);
            if (user is null) throw new NotFoundException("User not found");
            users.Add(user);
        }

        return users;
    }
}
