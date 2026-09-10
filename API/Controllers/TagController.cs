using Application.Auth.Authorization;
using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/tags")]
public class TagController : ControllerBase
{
    private readonly ILogger<TagController> _logger;
    private readonly ITagService _tagService;

    public TagController(
        ILogger<TagController> logger,
        ITagService tagService)
    {
        _logger = logger;
        _tagService = tagService;
    }

    [HttpGet]
    public async Task<IActionResult> GetTags()
    {
        _logger.LogInformation("Fetching tags.");
        var tags = await _tagService.GetTagsAsync();
        return Ok(tags);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetTag(Guid id)
    {
        _logger.LogInformation("Fetching tag with ID: {Id}", id);
        var tag = await _tagService.GetTagByIdAsync(id);
        return tag is null ? NotFound() : Ok(tag);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.CanManageProjects)]
    public async Task<IActionResult> CreateTag([FromBody] CreateTagDto tag)
    {
        _logger.LogInformation("Creating a new tag.");
        var createdTag = await _tagService.CreateTagAsync(tag);
        return CreatedAtAction(nameof(GetTag), new { id = createdTag.Id }, createdTag);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.CanManageProjects)]
    public async Task<IActionResult> UpdateTag(Guid id, [FromBody] UpdateTagDto tag)
    {
        _logger.LogInformation("Updating tag with ID: {Id}", id);
        var existingTag = await _tagService.GetTagByIdAsync(id);
        if (existingTag is null)
        {
            return NotFound();
        }

        await _tagService.UpdateTagAsync(id, tag);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.CanManageProjects)]
    public async Task<IActionResult> DeleteTag(Guid id)
    {
        _logger.LogInformation("Deleting tag with ID: {Id}", id);
        var existingTag = await _tagService.GetTagByIdAsync(id);
        if (existingTag is null)
        {
            return NotFound();
        }

        await _tagService.DeleteTagAsync(id);
        return NoContent();
    }
}
