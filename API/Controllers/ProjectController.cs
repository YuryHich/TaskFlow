using Application.Auth.Authorization;
using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/projects")]
public class ProjectController : ControllerBase
{
    private readonly ILogger<ProjectController> _logger;
    private readonly IProjectService _projectService;

    public ProjectController(
        ILogger<ProjectController> logger,
        IProjectService projectService)
    {
        _logger = logger;
        _projectService = projectService;
    }

    [HttpGet]
    public async Task<IActionResult> GetProjects()
    {
        _logger.LogInformation("Fetching projects.");
        var projects = await _projectService.GetProjectsAsync();
        return Ok(projects);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetProject(Guid id)
    {
        _logger.LogInformation("Fetching project with ID: {Id}", id);
        var project = await _projectService.GetProjectByIdAsync(id);
        return Ok(project);
    }

    [HttpGet("{projectId:guid}/tasks")]
    public async Task<IActionResult> GetProjectTasks(Guid projectId)
    {
        _logger.LogInformation("Fetching tasks for project with ID: {ProjectId}", projectId);
        var tasks = await _projectService.GetProjectTasksAsync(projectId);
        return Ok(tasks);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.CanManageProjects)]
    public async Task<IActionResult> CreateProject([FromBody] CreateProjectDto project)
    {
        _logger.LogInformation("Creating a new project.");
        var createdProject = await _projectService.CreateProjectAsync(project);
        return CreatedAtAction(nameof(GetProject), new { id = createdProject.Id }, createdProject);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateProject(Guid id, [FromBody] UpdateProjectDto project)
    {
        _logger.LogInformation("Updating project with ID: {Id}", id);
        await _projectService.UpdateProjectAsync(id, project);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.CanDeleteProjects)]
    public async Task<IActionResult> DeleteProject(Guid id)
    {
        _logger.LogInformation("Deleting project with ID: {Id}", id);
        await _projectService.DeleteProjectAsync(id);
        return NoContent();
    }
}
