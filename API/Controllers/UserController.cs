using Application.DTOs;
using Application.Interfaces;
using Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/users")]
public class UserController : ControllerBase
{
    private readonly ILogger<UserController> _logger;
    private readonly IUserService _userService;
    private readonly ICurrentUser _currentUser;

    public UserController(ILogger<UserController> logger, IUserService userService, ICurrentUser currentUser)
    {
        _logger = logger;
        _userService = userService;
        _currentUser = currentUser;
    }

    [HttpGet]
    [Authorize(Roles = $"{nameof(UserRole.Admin)},{nameof(UserRole.Manager)}")]
    public async Task<IActionResult> GetUsers()
    {
        _logger.LogInformation("Fetching users.");
        var users = await _userService.GetUsersAsync();
        return Ok(users);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = $"{nameof(UserRole.Admin)},{nameof(UserRole.Manager)}")]
    public async Task<IActionResult> GetUser(Guid id)
    {
        _logger.LogInformation("Fetching user with ID: {Id}", id);
        var user = await _userService.GetUserByIdAsync(id);
        return user is null ? NotFound() : Ok(user);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserDto user)
    {
        _logger.LogInformation("Updating user with ID: {Id}", id);
        if (id != _currentUser.UserId && _currentUser.Role != UserRole.Admin)
        {
            return Forbid();
        }

        var existingUser = await _userService.GetUserByIdAsync(id);
        if (existingUser is null)
        {
            return NotFound();
        }

        await _userService.UpdateUserAsync(id, user);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        _logger.LogInformation("Deleting user with ID: {Id}", id);
        var existingUser = await _userService.GetUserByIdAsync(id);
        if (existingUser is null)
        {
            return NotFound();
        }

        await _userService.DeleteUserAsync(id);
        return NoContent();
    }
}
