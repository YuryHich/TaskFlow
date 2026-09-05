using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.DTOs;
using Application.Interfaces;
using Domain.Models;
using Domain.Repositories;
using Mapster;

namespace Application.Services;

    public class ProjectService : IProjectService
    {
        private readonly IProjectRepository _projectRepository;

        public ProjectService(IProjectRepository projectRepository)
        {
            _projectRepository = projectRepository;
        }

        public async Task<IEnumerable<ProjectDto>> GetProjectsAsync()
        {
            var projects = await _projectRepository.GetProjectsAsync();
            return projects.Adapt<IEnumerable<ProjectDto>>();
        }

        public async Task<ProjectDto?> GetProjectByIdAsync(Guid id)
        {
            var project = await _projectRepository.GetProjectByIdAsync(id);
            return project?.Adapt<ProjectDto>();
        }

        public async Task<IEnumerable<TaskDto>> GetProjectTasksAsync(Guid projectId)
        {
            var tasks = await _projectRepository.GetProjectTasksAsync(projectId);
            return tasks.Adapt<IEnumerable<TaskDto>>();
        }

        public async Task<ProjectDto> CreateProjectAsync(CreateProjectDto project)
        {
            var projectEntity = project.Adapt<Project>();
            projectEntity.Id = Guid.NewGuid();
            projectEntity.CreatedAt = DateTime.UtcNow;
            await _projectRepository.CreateProjectAsync(projectEntity);
            return projectEntity.Adapt<ProjectDto>();
        }

        public async Task UpdateProjectAsync(Guid id, UpdateProjectDto project)
        {
            var projectEntity = await _projectRepository.GetProjectByIdAsync(id);
            if (projectEntity is null)
            {
                return;
            }

            project.Adapt(projectEntity);

            await _projectRepository.UpdateProjectAsync(projectEntity);
        }

        public async Task DeleteProjectAsync(Guid id)
        {
            await _projectRepository.DeleteProjectAsync(id);
        }
    }
