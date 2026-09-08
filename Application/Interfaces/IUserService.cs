using Application.DTOs;

namespace Application.Interfaces;

public interface IUserService
{
    Task<IEnumerable<UserDto>> GetUsersAsync();
    Task<UserDto?> GetUserByIdAsync(Guid id);
    Task UpdateUserAsync(Guid id, UpdateUserDto user);
    Task DeleteUserAsync(Guid id);
}