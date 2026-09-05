using Application.DTOs;
using Application.Interfaces;
using Domain.Models;
using Domain.Repositories;
using Mapster;

namespace Application.Services;

public class CommentService : ICommentService
{
    private readonly ICommentRepository _commentRepository;

    public CommentService(ICommentRepository commentRepository)
    {
        _commentRepository = commentRepository;
    }

    public async Task<IEnumerable<CommentDto>> GetCommentsAsync()
    {
        var comments = await _commentRepository.GetCommentsAsync();
        return comments.Adapt<IEnumerable<CommentDto>>();
    }

    public async Task<CommentDto?> GetCommentByIdAsync(Guid id)
    {
        var comment = await _commentRepository.GetCommentByIdAsync(id);
        return comment?.Adapt<CommentDto>();
    }

    public async Task<IEnumerable<CommentDto>> GetTaskCommentsAsync(Guid taskId)
    {
        var comments = await _commentRepository.GetTaskCommentsAsync(taskId);
        return comments.Adapt<IEnumerable<CommentDto>>();
    }

    public async Task<CommentDto> CreateCommentAsync(CreateCommentDto comment)
    {
        var commentEntity = comment.Adapt<Comment>();
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
            return;
        }

        comment.Adapt(commentEntity);
        await _commentRepository.UpdateCommentAsync(commentEntity);
    }

    public async Task DeleteCommentAsync(Guid id)
    {
        await _commentRepository.DeleteCommentAsync(id);
    }
}