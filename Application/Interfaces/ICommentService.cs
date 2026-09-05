using Application.DTOs;

namespace Application.Interfaces;

public interface ICommentService
{
    Task<IEnumerable<CommentDto>> GetCommentsAsync();
    Task<CommentDto?> GetCommentByIdAsync(Guid id);
    Task<IEnumerable<CommentDto>> GetTaskCommentsAsync(Guid taskId);
    Task<CommentDto> CreateCommentAsync(CreateCommentDto comment);
    Task UpdateCommentAsync(Guid id, UpdateCommentDto comment);
    Task DeleteCommentAsync(Guid id);
}