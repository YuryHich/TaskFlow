using Application.DTOs;
using Application.Interfaces;
using Domain.Repositories;
using Mapster;

namespace Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;

    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<IEnumerable<UserDto>> GetUsersAsync()
    {
        var users = await _userRepository.GetUsersAsync();
        return users.Adapt<IEnumerable<UserDto>>();
    }

    public async Task<UserDto?> GetUserByIdAsync(Guid id)
    {
        var user = await _userRepository.GetUserByIdAsync(id);
        return user?.Adapt<UserDto>();
    }

    public async Task UpdateUserAsync(Guid id, UpdateUserDto user)
    {
        var userEntity = await _userRepository.GetUserByIdAsync(id);
        if (userEntity is null)
        {
            return;
        }

        user.Adapt(userEntity);
        await _userRepository.UpdateUserAsync(userEntity);
    }

    public async Task DeleteUserAsync(Guid id)
    {
        await _userRepository.DeleteUserAsync(id);
    }
}