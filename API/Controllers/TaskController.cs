using Domain.Comments;
using Domain.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/tasks")]
public class TaskController : ControllerBase
{
    private readonly ILogger<TaskController> _logger;

    public TaskController(ILogger<TaskController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetTasks()
    {
        _logger.LogInformation("Fetching tasks.");
        // Implementation for fetching tasks
        return Ok();
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetTask(Guid id)
    {
        _logger.LogInformation("Fetching task with ID: {Id}", id);
        // Implementation for fetching a specific task
        return Ok();
    }

    [HttpGet("{taskId:guid}/comments")]
    public async Task<IActionResult> GetTaskComments(Guid taskId)
    {
        _logger.LogInformation("Fetching comments for task with ID: {TaskId}", taskId);
        // Implementation for fetching comments of a task
        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> CreateTask([FromBody] CreateTaskDto task)
    {
        _logger.LogInformation("Creating a new task.");
        // Implementation for creating a new task
        return CreatedAtAction(nameof(GetTask), new { id = Guid.Empty }, task);
    }

    [HttpPost("{taskId:guid}/comments")]
    public async Task<IActionResult> CreateTaskComment(Guid taskId, [FromBody] CreateCommentDto comment)
    {
        _logger.LogInformation("Creating a comment for task with ID: {TaskId}", taskId);
        // Implementation for creating a comment for a task
        return CreatedAtAction(nameof(GetTaskComments), new { taskId }, comment);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateTask(Guid id, [FromBody] UpdateTaskDto task)
    {
        _logger.LogInformation("Updating task with ID: {Id}", id);
        // Implementation for updating a task
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteTask(Guid id)
    {
        _logger.LogInformation("Deleting task with ID: {Id}", id);
        // Implementation for deleting a task
        return NoContent();
    }
}
