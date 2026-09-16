using Domain.Models;

namespace Domain.Repositories;

public interface IProjectRepository
{
    Task<IEnumerable<Project>> GetProjectsAsync(Guid? accessibleByUserId = null);
    Task<Project?> GetProjectByIdAsync(Guid id);
    Task<bool> UserHasProjectReadAccessAsync(Guid projectId, Guid userId);
    Task<IEnumerable<WorkTask>> GetProjectTasksAsync(Guid projectId);
    Task CreateProjectAsync(Project project);
    Task UpdateProjectAsync(Project project);
    Task DeleteProjectAsync(Guid id);
}
