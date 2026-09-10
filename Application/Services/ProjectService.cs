using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Auth.Authorization;
using Application.DTOs;
using Application.Exceptions;
using Application.Interfaces;
using Domain.Models;
using Domain.Repositories;
using Mapster;
using Microsoft.AspNetCore.Authorization;

namespace Application.Services;

    public class ProjectService : IProjectService
    {
    private readonly IProjectRepository _projectRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUser _currentUser;
    private readonly IAuthorizationService _authorizationService;

    public ProjectService(
        IProjectRepository projectRepository,
        IUserRepository userRepository,
        ICurrentUser currentUser,
        IAuthorizationService authorizationService)
        {
        _projectRepository = projectRepository;
        _userRepository = userRepository;
        _currentUser = currentUser;
        _authorizationService = authorizationService;
        }

        public async Task<IEnumerable<ProjectDto>> GetProjectsAsync()
        {
        var filter = _currentUser.IsAdminOrManager ? (Guid?)null : _currentUser.UserId;
        var projects = await _projectRepository.GetProjectsAsync(filter);
        return projects.Adapt<IEnumerable<ProjectDto>>();
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
        var ownerId = project.OwnerId is Guid id && id != Guid.Empty ? id : _currentUser.UserId;
        if (ownerId != _currentUser.UserId)
            await EnsureUserExistsAsync(ownerId);
        projectEntity.OwnerId = ownerId;
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
            project.Adapt(projectEntity);
            if (project.OwnerId is Guid newOwnerId && newOwnerId != Guid.Empty && newOwnerId != projectEntity.OwnerId)
            {
                if (!_currentUser.IsAdminOrManager)
                    throw new ForbiddenException("You are not authorized to change the project owner");
                await EnsureUserExistsAsync(newOwnerId);
                projectEntity.OwnerId = newOwnerId;
            }
            await _projectRepository.UpdateProjectAsync(projectEntity);
        }

    public async Task DeleteProjectAsync(Guid id)
    {
        var projectEntity = await _projectRepository.GetProjectByIdAsync(id);
        if (projectEntity is null) throw new NotFoundException("Project not found");
        await _projectRepository.DeleteProjectAsync(id);
        }

    private async Task EnsureUserExistsAsync(Guid userId)
    {
        var user = await _userRepository.GetUserByIdAsync(userId);
        if (user is null) throw new NotFoundException("User not found");
    }
    }
