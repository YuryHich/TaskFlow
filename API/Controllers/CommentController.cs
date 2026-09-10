using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/comments")]
public class CommentController : ControllerBase
{
    private readonly ILogger<CommentController> _logger;
    private readonly ICommentService _commentService;

    public CommentController(
        ILogger<CommentController> logger,
        ICommentService commentService)
    {
        _logger = logger;
        _commentService = commentService;
    }

    [HttpGet]
    public async Task<IActionResult> GetComments()
    {
        _logger.LogInformation("Fetching comments.");
        var comments = await _commentService.GetCommentsAsync();
        return Ok(comments);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetComment(Guid id)
    {
        _logger.LogInformation("Fetching comment with ID: {Id}", id);
        var comment = await _commentService.GetCommentByIdAsync(id);
        return Ok(comment);
    }

    [HttpPost]
    public async Task<IActionResult> CreateComment([FromBody] CreateCommentDto comment)
    {
        _logger.LogInformation("Creating a new comment.");
        var createdComment = await _commentService.CreateCommentAsync(comment);
        return CreatedAtAction(nameof(GetComment), new { id = createdComment.Id }, createdComment);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateComment(Guid id, [FromBody] UpdateCommentDto comment)
    {
        _logger.LogInformation("Updating comment with ID: {Id}", id);
        await _commentService.UpdateCommentAsync(id, comment);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteComment(Guid id)
    {
        _logger.LogInformation("Deleting comment with ID: {Id}", id);
        await _commentService.DeleteCommentAsync(id);
        return NoContent();
    }
}
