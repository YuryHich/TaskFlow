using Domain.Models;
using Domain.Repositories;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class EfCommentRepository : ICommentRepository
{
    private readonly AppDbContext _context;

    public EfCommentRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Comment>> GetCommentsAsync()
    {
        return await _context.Comments
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Comment?> GetCommentByIdAsync(Guid id)
    {
        return await _context.Comments.FindAsync(id);
    }

    public async Task<IEnumerable<Comment>> GetTaskCommentsAsync(Guid taskId)
    {
        return await _context.Comments
            .AsNoTracking()
            .Where(comment => comment.TaskId == taskId)
            .ToListAsync();
    }

    public async Task CreateCommentAsync(Comment comment)
    {
        _context.Comments.Add(comment);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateCommentAsync(Comment comment)
    {
        await _context.SaveChangesAsync();
    }

    public async Task DeleteCommentAsync(Guid id)
    {
        var comment = await _context.Comments.FindAsync(id);
        if (comment is not null)
        {
            _context.Comments.Remove(comment);
        }

        await _context.SaveChangesAsync();
    }
}
