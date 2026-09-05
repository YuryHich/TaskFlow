using Domain.Models;
using Domain.Repositories;

namespace Infrastructure.Repositories;

public class InMemoryProjectRepository : IProjectRepository
{
    private readonly List<Project> _projects = [];

    public Task<IEnumerable<Project>> GetProjectsAsync()
    {
        return Task.FromResult<IEnumerable<Project>>(_projects.ToList());
    }

    public Task<Project?> GetProjectByIdAsync(Guid id)
    {
        var project = _projects.FirstOrDefault(p => p.Id == id);
        return Task.FromResult(project);
    }

    public Task<IEnumerable<WorkTask>> GetProjectTasksAsync(Guid projectId)
    {
        var project = _projects.FirstOrDefault(p => p.Id == projectId);
        IEnumerable<WorkTask> tasks = project?.Tasks.ToList() ?? [];
        return Task.FromResult(tasks);
    }

    public Task CreateProjectAsync(Project project)
    {
        _projects.Add(project);
        return Task.CompletedTask;
    }

    public Task UpdateProjectAsync(Project project)
    {
        var existingProject = _projects.FirstOrDefault(p => p.Id == project.Id);
        if (existingProject is not null)
        {
            existingProject.Name = project.Name;
            existingProject.Description = project.Description;
            existingProject.OwnerId = project.OwnerId;
        }

        return Task.CompletedTask;
    }

    public Task DeleteProjectAsync(Guid id)
    {
        _projects.RemoveAll(p => p.Id == id);
        return Task.CompletedTask;
    }
}
