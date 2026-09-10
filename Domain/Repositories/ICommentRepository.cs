using Domain.Models;

namespace Domain.Repositories
{
    public interface ICommentRepository
    {
        public Task<IEnumerable<Comment>> GetCommentsAsync(Guid? accessibleByUserId = null);
        public Task<Comment?> GetCommentByIdAsync(Guid id);
        public Task<IEnumerable<Comment>> GetTaskCommentsAsync(Guid taskId);
        public Task CreateCommentAsync(Comment comment);
        public Task UpdateCommentAsync(Comment comment);
        public Task DeleteCommentAsync(Guid id);
    }
}