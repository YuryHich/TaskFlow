using Application.DTOs;
using Application.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/tasks")]
public class TaskController : ControllerBase
{
    private readonly ILogger<TaskController> _logger;
    private readonly ITaskService _taskService;
    private readonly ICommentService _commentService;
    private readonly IValidator<CreateTaskDto> _createTaskValidator;
    private readonly IValidator<UpdateTaskDto> _updateTaskValidator;
    private readonly IValidator<CreateCommentDto> _createCommentValidator;

    public TaskController(
        ILogger<TaskController> logger,
        ITaskService taskService,
        ICommentService commentService,
        IValidator<CreateTaskDto> createTaskValidator,
        IValidator<UpdateTaskDto> updateTaskValidator,
        IValidator<CreateCommentDto> createCommentValidator)
    {
        _logger = logger;
        _taskService = taskService;
        _commentService = commentService;
        _createTaskValidator = createTaskValidator;
        _updateTaskValidator = updateTaskValidator;
        _createCommentValidator = createCommentValidator;
    }

    [HttpGet]
    public async Task<IActionResult> GetTasks()
    {
        _logger.LogInformation("Fetching tasks.");
        var tasks = await _taskService.GetTasksAsync();
        return Ok(tasks);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetTask(Guid id)
    {
        _logger.LogInformation("Fetching task with ID: {Id}", id);
        var task = await _taskService.GetTaskByIdAsync(id);
        return Ok(task);
    }

    [HttpGet("{taskId:guid}/comments")]
    public async Task<IActionResult> GetTaskComments(Guid taskId)
    {
        _logger.LogInformation("Fetching comments for task with ID: {TaskId}", taskId);
        var comments = await _taskService.GetTaskCommentsAsync(taskId);
        return Ok(comments);
    }

    [HttpPost]
    public async Task<IActionResult> CreateTask([FromBody] CreateTaskDto task)
    {
        _logger.LogInformation("Creating a new task.");
        var validationResult = await _createTaskValidator.ValidateAsync(task);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.Errors);
        }

        var createdTask = await _taskService.CreateTaskAsync(task);
        return CreatedAtAction(nameof(GetTask), new { id = createdTask.Id }, createdTask);
    }

    [HttpPost("{taskId:guid}/comments")]
    public async Task<IActionResult> CreateTaskComment(Guid taskId, [FromBody] CreateCommentDto comment)
    {
        _logger.LogInformation("Creating a comment for task with ID: {TaskId}", taskId);
        if (comment.TaskId != taskId)
        {
            return BadRequest("TaskId in the route and request body must match.");
        }

        var validationResult = await _createCommentValidator.ValidateAsync(comment);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.Errors);
        }

        var createdComment = await _commentService.CreateCommentAsync(comment);
        return CreatedAtAction(nameof(GetTaskComments), new { taskId }, createdComment);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateTask(Guid id, [FromBody] UpdateTaskDto task)
    {
        _logger.LogInformation("Updating task with ID: {Id}", id);
        var validationResult = await _updateTaskValidator.ValidateAsync(task);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.Errors);
        }

        await _taskService.UpdateTaskAsync(id, task);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteTask(Guid id)
    {
        _logger.LogInformation("Deleting task with ID: {Id}", id);
        await _taskService.DeleteTaskAsync(id);
        return NoContent();
    }
}
