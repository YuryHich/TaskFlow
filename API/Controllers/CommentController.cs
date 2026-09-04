using Domain.Comments;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/comments")]
public class CommentController : ControllerBase
{
    private readonly ILogger<CommentController> _logger;

    public CommentController(ILogger<CommentController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetComments()
    {
        _logger.LogInformation("Fetching comments.");
        // Implementation for fetching comments
        return Ok();
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetComment(Guid id)
    {
        _logger.LogInformation("Fetching comment with ID: {Id}", id);
        // Implementation for fetching a specific comment
        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> CreateComment([FromBody] CreateCommentDto comment)
    {
        _logger.LogInformation("Creating a new comment.");
        // Implementation for creating a new comment
        return CreatedAtAction(nameof(GetComment), new { id = Guid.Empty }, comment);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateComment(Guid id, [FromBody] UpdateCommentDto comment)
    {
        _logger.LogInformation("Updating comment with ID: {Id}", id);
        // Implementation for updating a comment
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteComment(Guid id)
    {
        _logger.LogInformation("Deleting comment with ID: {Id}", id);
        // Implementation for deleting a comment
        return NoContent();
    }
}
