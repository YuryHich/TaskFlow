using Domain.Tags;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/tags")]
public class TagController : ControllerBase
{
    private readonly ILogger<TagController> _logger;

    public TagController(ILogger<TagController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetTags()
    {
        _logger.LogInformation("Fetching tags.");
        // Implementation for fetching tags
        return Ok();
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetTag(Guid id)
    {
        _logger.LogInformation("Fetching tag with ID: {Id}", id);
        // Implementation for fetching a specific tag
        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> CreateTag([FromBody] CreateTagDto tag)
    {
        _logger.LogInformation("Creating a new tag.");
        // Implementation for creating a new tag
        return CreatedAtAction(nameof(GetTag), new { id = Guid.Empty }, tag);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateTag(Guid id, [FromBody] UpdateTagDto tag)
    {
        _logger.LogInformation("Updating tag with ID: {Id}", id);
        // Implementation for updating a tag
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteTag(Guid id)
    {
        _logger.LogInformation("Deleting tag with ID: {Id}", id);
        // Implementation for deleting a tag
        return NoContent();
    }
}
