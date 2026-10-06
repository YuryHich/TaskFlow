using Application.Auth.Authorization;
using Application.DTOs;
using Application.Events;
using Application.Exceptions;
using Application.Interfaces;
using Domain.Models;
using Domain.Repositories;
using Mapster;
using Microsoft.AspNetCore.Authorization;

namespace Application.Services;

public class CommentService : ICommentService
{
    private readonly ICommentRepository _commentRepository;
    private readonly ITaskRepository _taskRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly ICurrentUser _currentUser;
    private readonly IAuthorizationService _authorizationService;
    private readonly IAppEventPublisher _appEventPublisher;
    private readonly IProjectAudience _projectAudience;

    public CommentService(
        ICommentRepository commentRepository,
        ITaskRepository taskRepository,
        IProjectRepository projectRepository,
        ICurrentUser currentUser,
        IAuthorizationService authorizationService,
        IAppEventPublisher appEventPublisher,
        IProjectAudience projectAudience)
    {
        _commentRepository = commentRepository;
        _taskRepository = taskRepository;
        _projectRepository = projectRepository;
        _currentUser = currentUser;
        _authorizationService = authorizationService;
        _appEventPublisher = appEventPublisher;
        _projectAudience = projectAudience;
    }

    public async Task<IEnumerable<CommentDto>> GetCommentsAsync()
    {
        var filter = _currentUser.IsAdminOrManager ? (Guid?)null : _currentUser.UserId;
        var comments = await _commentRepository.GetCommentsAsync(filter);
        return comments.Adapt<IEnumerable<CommentDto>>();
    }

    public async Task<CommentDto?> GetCommentByIdAsync(Guid id)
    {
        var comment = await _commentRepository.GetCommentByIdAsync(id);
        if (comment is null) throw new NotFoundException("Comment not found");
        await EnsureTaskAccessAsync(comment.TaskId);
        return comment.Adapt<CommentDto>();
    }

    public async Task<IEnumerable<CommentDto>> GetTaskCommentsAsync(Guid taskId)
    {
        await EnsureTaskAccessAsync(taskId);
        var comments = await _commentRepository.GetTaskCommentsAsync(taskId);
        return comments.Adapt<IEnumerable<CommentDto>>();
    }

    public async Task<CommentDto> CreateCommentAsync(CreateCommentDto comment)
    {
        var task = await EnsureTaskAccessAsync(comment.TaskId);
        var commentEntity = comment.Adapt<Comment>();
        commentEntity.AuthorId = _currentUser.UserId;
        commentEntity.Id = Guid.NewGuid();
        commentEntity.CreatedAt = DateTime.UtcNow;

        await _commentRepository.CreateCommentAsync(commentEntity);
        var audience = await _projectAudience.GetUserIdsAsync(task.ProjectId);
        await _appEventPublisher.PublishAsync(new CommentAddedEvent(
            EventId: Guid.NewGuid(),
            OccurredAt: DateTime.UtcNow,
            CommentId: commentEntity.Id,
            TaskId: commentEntity.TaskId,
            ProjectId: task.ProjectId,
            ActorUserId: _currentUser.UserId,
            AudienceUserIds: audience));
        return commentEntity.Adapt<CommentDto>();
    }

    public async Task UpdateCommentAsync(Guid id, UpdateCommentDto comment)
    {
        var commentEntity = await _commentRepository.GetCommentByIdAsync(id);
        if (commentEntity is null)
        {
            throw new NotFoundException("Comment not found");
        }

        var task = await EnsureTaskAccessAsync(commentEntity.TaskId);
        comment.Adapt(commentEntity);
        await _commentRepository.UpdateCommentAsync(commentEntity);
        var audience = await _projectAudience.GetUserIdsAsync(task.ProjectId);
        await _appEventPublisher.PublishAsync(new CommentUpdatedEvent(
            EventId: Guid.NewGuid(),
            OccurredAt: DateTime.UtcNow,
            CommentId: commentEntity.Id,
            TaskId: commentEntity.TaskId,
            ProjectId: task.ProjectId,
            ActorUserId: _currentUser.UserId,
            AudienceUserIds: audience));
    }

    public async Task DeleteCommentAsync(Guid id)
    {
        var commentEntity = await _commentRepository.GetCommentByIdAsync(id);
        if (commentEntity is null) throw new NotFoundException("Comment not found");
        var task = await EnsureTaskAccessAsync(commentEntity.TaskId);
        var audience = await _projectAudience.GetUserIdsAsync(task.ProjectId);
        await _commentRepository.DeleteCommentAsync(id);
        await _appEventPublisher.PublishAsync(new CommentDeletedEvent(
            EventId: Guid.NewGuid(),
            OccurredAt: DateTime.UtcNow,
            CommentId: commentEntity.Id,
            TaskId: commentEntity.TaskId,
            ProjectId: task.ProjectId,
            ActorUserId: _currentUser.UserId,
            AudienceUserIds: audience));
    }

    private async Task<WorkTask> EnsureTaskAccessAsync(Guid taskId)
    {
        var task = await _taskRepository.GetTaskByIdAsync(taskId);
        if (task is null) throw new NotFoundException("Task not found");
        var authorizationResult = await _authorizationService.AuthorizeTaskAccessAsync(_currentUser.User, task);
        if (!authorizationResult.Succeeded) throw new ForbiddenException("You are not authorized to access this task");
        return task;
    }
}
