using Domain.Projects;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/projects")]
public class ProjectController : ControllerBase
{
    private readonly ILogger<ProjectController> _logger;

    public ProjectController(ILogger<ProjectController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetProjects()
    {
        _logger.LogInformation("Fetching projects.");
        // Implementation for fetching projects
        return Ok();
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetProject(Guid id)
    {
        _logger.LogInformation("Fetching project with ID: {Id}", id);
        // Implementation for fetching a specific project
        return Ok();
    }

    [HttpGet("{projectId:guid}/tasks")]
    public async Task<IActionResult> GetProjectTasks(Guid projectId)
    {
        _logger.LogInformation("Fetching tasks for project with ID: {ProjectId}", projectId);
        // Implementation for fetching tasks of a project
        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> CreateProject([FromBody] CreateProjectDto project)
    {
        _logger.LogInformation("Creating a new project.");
        // Implementation for creating a new project
        return CreatedAtAction(nameof(GetProject), new { id = Guid.Empty }, project);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateProject(Guid id, [FromBody] UpdateProjectDto project)
    {
        _logger.LogInformation("Updating project with ID: {Id}", id);
        // Implementation for updating a project
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteProject(Guid id)
    {
        _logger.LogInformation("Deleting project with ID: {Id}", id);
        // Implementation for deleting a project
        return NoContent();
    }
}
