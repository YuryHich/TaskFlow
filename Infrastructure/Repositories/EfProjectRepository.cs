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
    public async Task<IEnumerable<Project>> GetProjectsAsync()
    {
        return await _context.Projects.AsNoTracking().ToListAsync();
    }

    public async Task<Project?> GetProjectByIdAsync(Guid id)
    {
        return await _context.Projects.FindAsync(id);
    }

    public async Task<IEnumerable<WorkTask>> GetProjectTasksAsync(Guid projectId)
    {
        return await _context.Tasks.AsNoTracking().Where(task => task.ProjectId == projectId).ToListAsync();
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
