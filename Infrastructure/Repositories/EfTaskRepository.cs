using Domain.Models;
using Domain.Repositories;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class EfTaskRepository : ITaskRepository
{
    private readonly AppDbContext _context;

    public EfTaskRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<WorkTask>> GetTasksAsync(Guid? accessibleByUserId = null)
    {
        var query = _context.Tasks.AsNoTracking();
        if (accessibleByUserId is Guid userId)
        {
            query = query.Where(task =>
                task.Project.OwnerId == userId
                || task.Project.Tasks.Any(projectTask =>
                    projectTask.Assignees.Any(assignee => assignee.Id == userId)));
        }

        return await query
            .Include(task => task.Assignees)
            .ToListAsync();
    }

    public async Task<WorkTask?> GetTaskByIdAsync(Guid id)
    {
        return await _context.Tasks
            .Include(task => task.Assignees)
            .FirstOrDefaultAsync(task => task.Id == id);
    }

    public async Task<IEnumerable<Comment>> GetTaskCommentsAsync(Guid taskId)
    {
        return await _context.Comments
            .AsNoTracking()
            .Where(comment => comment.TaskId == taskId)
            .ToListAsync();
    }

    public async Task CreateTaskAsync(WorkTask task)
    {
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateTaskAsync(WorkTask task)
    {
        await _context.SaveChangesAsync();
    }

    public async Task DeleteTaskAsync(Guid id)
    {
        var task = await _context.Tasks.FindAsync(id);
        if (task is not null)
        {
            _context.Tasks.Remove(task);
        }

        await _context.SaveChangesAsync();
    }
}
