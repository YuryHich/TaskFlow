using Application.Auth.Authorization;
using Application.DTOs;
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

    public CommentService(
        ICommentRepository commentRepository,
        ITaskRepository taskRepository,
        IProjectRepository projectRepository,
        ICurrentUser currentUser,
        IAuthorizationService authorizationService)
    {
        _commentRepository = commentRepository;
        _taskRepository = taskRepository;
        _projectRepository = projectRepository;
        _currentUser = currentUser;
        _authorizationService = authorizationService;
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
        await EnsureTaskAccessAsync(comment.TaskId);
        var commentEntity = comment.Adapt<Comment>();
        commentEntity.AuthorId = _currentUser.UserId;
        commentEntity.Id = Guid.NewGuid();
        commentEntity.CreatedAt = DateTime.UtcNow;

        await _commentRepository.CreateCommentAsync(commentEntity);
        return commentEntity.Adapt<CommentDto>();
    }

    public async Task UpdateCommentAsync(Guid id, UpdateCommentDto comment)
    {
        var commentEntity = await _commentRepository.GetCommentByIdAsync(id);
        if (commentEntity is null)
        {
            throw new NotFoundException("Comment not found");
        }

        await EnsureTaskAccessAsync(commentEntity.TaskId);
        comment.Adapt(commentEntity);
        await _commentRepository.UpdateCommentAsync(commentEntity);
    }

    public async Task DeleteCommentAsync(Guid id)
    {
        var commentEntity = await _commentRepository.GetCommentByIdAsync(id);
        if (commentEntity is null) throw new NotFoundException("Comment not found");
        await EnsureTaskAccessAsync(commentEntity.TaskId);
        await _commentRepository.DeleteCommentAsync(id);
    }

    private async Task EnsureTaskAccessAsync(Guid taskId)
    {
        var task = await _taskRepository.GetTaskByIdAsync(taskId);
        if (task is null) throw new NotFoundException("Task not found");
        var authorizationResult = await _authorizationService.AuthorizeTaskAccessAsync(_currentUser.User, task);
        if (!authorizationResult.Succeeded) throw new ForbiddenException("You are not authorized to access this task");
    }
}
