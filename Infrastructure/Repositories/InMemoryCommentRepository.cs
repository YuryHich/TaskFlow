using Domain.Models;
using Domain.Repositories;

namespace Infrastructure.Repositories;

public class InMemoryCommentRepository : ICommentRepository
{
    private readonly List<Comment> _comments = [];

    public Task<IEnumerable<Comment>> GetCommentsAsync(Guid? accessibleByUserId = null)
    {
        return Task.FromResult<IEnumerable<Comment>>(_comments.ToList());
    }

    public Task<Comment?> GetCommentByIdAsync(Guid id)
    {
        var comment = _comments.FirstOrDefault(c => c.Id == id);
        return Task.FromResult(comment);
    }

    public Task<IEnumerable<Comment>> GetTaskCommentsAsync(Guid taskId)
    {
        var comments = _comments.Where(c => c.TaskId == taskId).ToList();
        return Task.FromResult<IEnumerable<Comment>>(comments);
    }

    public Task CreateCommentAsync(Comment comment)
    {
        _comments.Add(comment);
        return Task.CompletedTask;
    }

    public Task UpdateCommentAsync(Comment comment)
    {
        var existingComment = _comments.FirstOrDefault(c => c.Id == comment.Id);
        if (existingComment is not null)
        {
            existingComment.TaskId = comment.TaskId;
            existingComment.AuthorId = comment.AuthorId;
            existingComment.Content = comment.Content;
        }

        return Task.CompletedTask;
    }

    public Task DeleteCommentAsync(Guid id)
    {
        _comments.RemoveAll(c => c.Id == id);
        return Task.CompletedTask;
    }
}