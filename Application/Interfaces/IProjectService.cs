using Application.DTOs;

namespace Application.Interfaces;

    public interface IProjectService
    {
        Task<IEnumerable<ProjectDto>> GetProjectsAsync();
        Task<ProjectDto?> GetProjectByIdAsync(Guid id);
        Task<IEnumerable<TaskDto>> GetProjectTasksAsync(Guid projectId);
        Task<ProjectDto> CreateProjectAsync(CreateProjectDto project);
        Task UpdateProjectAsync(Guid id, UpdateProjectDto project);
        Task DeleteProjectAsync(Guid id);
    }
