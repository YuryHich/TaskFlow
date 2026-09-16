using Domain.Models;
using Domain.Repositories;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;


public class EfProjectRepository : IProjectRepository
{
    private readonly AppDbContext _context;

    public EfProjectRepository(AppDbContext context)
    {
        _context = context;
    }
    public async Task<IEnumerable<Project>> GetProjectsAsync(Guid? accessibleByUserId = null)
    {
        var query = _context.Projects.AsNoTracking();
        if (accessibleByUserId is Guid userId)
        {
            query = query.Where(project =>
                project.OwnerId == userId
                || project.Tasks.Any(task => task.Assignees.Any(assignee => assignee.Id == userId)));
        }

        return await query.ToListAsync();
    }

    public async Task<Project?> GetProjectByIdAsync(Guid id)
    {
        return await _context.Projects.FindAsync(id);
    }

    public Task<bool> UserHasProjectReadAccessAsync(Guid projectId, Guid userId)
    {
        return _context.Projects.AsNoTracking().AnyAsync(project =>
            project.Id == projectId
            && (project.OwnerId == userId
                || project.Tasks.Any(task => task.Assignees.Any(assignee => assignee.Id == userId))));
    }

    public async Task<IEnumerable<WorkTask>> GetProjectTasksAsync(Guid projectId)
    {
        return await _context.Tasks
            .AsNoTracking()
            .Include(task => task.Assignees)
            .Where(task => task.ProjectId == projectId)
            .ToListAsync();
    }

    public async Task CreateProjectAsync(Project project)
    {
         _context.Projects.Add(project);
         await _context.SaveChangesAsync();
    }

    public async Task UpdateProjectAsync(Project project)
    {
        await _context.SaveChangesAsync();
    }

    public async Task DeleteProjectAsync(Guid id)
    {
        var project = await _context.Projects.FindAsync(id);
        if (project != null)
        {
            _context.Projects.Remove(project);
        }
        await _context.SaveChangesAsync();
    }
}
