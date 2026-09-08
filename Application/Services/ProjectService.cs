using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Auth.Authorization;
using Application.DTOs;
using Application.Interfaces;
using Domain.Models;
using Domain.Repositories;
using Mapster;
using Microsoft.AspNetCore.Authorization;

namespace Application.Services;

    public class ProjectService : IProjectService
    {
    private readonly IProjectRepository _projectRepository;
    private readonly ICurrentUser _currentUser;
    private readonly IAuthorizationService _authorizationService;

    public ProjectService(
        IProjectRepository projectRepository,
        ICurrentUser currentUser,
        IAuthorizationService authorizationService)
        {
        _projectRepository = projectRepository;
        _currentUser = currentUser;
        _authorizationService = authorizationService;
        }

        public async Task<IEnumerable<ProjectDto>> GetProjectsAsync()
        {
        var projects = await _projectRepository.GetProjectsAsync();
        if (_currentUser.IsAdminOrManager)
        {
            return projects.Adapt<IEnumerable<ProjectDto>>();
        }
        else
        {
            return projects.Where(p => p.OwnerId == _currentUser.UserId).Adapt<IEnumerable<ProjectDto>>();
        }
        }

        public async Task<ProjectDto?> GetProjectByIdAsync(Guid id)
        {
        var project = await _projectRepository.GetProjectByIdAsync(id);
        if (project is null) { throw new NotFoundException("Project not found"); }
        var authorizationResult = await _authorizationService.AuthorizeProjectOwnerAsync(_currentUser.User, project);
        if (!authorizationResult.Succeeded) { throw new ForbiddenException("You are not authorized to access this project"); }
        return project.Adapt<ProjectDto>();
        }

        public async Task<IEnumerable<TaskDto>> GetProjectTasksAsync(Guid projectId)
    {
        var project = await _projectRepository.GetProjectByIdAsync(projectId);
        if (project is null) throw new NotFoundException("Project not found");
        var authorizationResult = await _authorizationService.AuthorizeProjectOwnerAsync(_currentUser.User, project);
        if (!authorizationResult.Succeeded) throw new ForbiddenException("You are not authorized to access this project");
        var tasks = await _projectRepository.GetProjectTasksAsync(projectId);
        return tasks.Adapt<IEnumerable<TaskDto>>();
        }

        public async Task<ProjectDto> CreateProjectAsync(CreateProjectDto project)
        {
        var projectEntity = project.Adapt<Project>();
        projectEntity.OwnerId = _currentUser.UserId;
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
                throw new NotFoundException("Project not found");
            }
            var authorizationResult = await _authorizationService.AuthorizeProjectOwnerAsync(_currentUser.User, projectEntity);
            if (!authorizationResult.Succeeded) throw new ForbiddenException("You are not authorized to update this project");
            var ownerId = projectEntity.OwnerId;
            project.Adapt(projectEntity);
            projectEntity.OwnerId = ownerId;
            await _projectRepository.UpdateProjectAsync(projectEntity);
        }

    public async Task DeleteProjectAsync(Guid id)
    {
        var projectEntity = await _projectRepository.GetProjectByIdAsync(id);
        if (projectEntity is null) throw new NotFoundException("Project not found");
        await _projectRepository.DeleteProjectAsync(id);
        }
    }
