using Domain.Models;
using Domain.Repositories;

namespace Infrastructure.Repositories;

public class InMemoryUserRepository : IUserRepository
{
    private readonly List<User> _users = [];

    public Task<IEnumerable<User>> GetUsersAsync()
    {
        return Task.FromResult<IEnumerable<User>>(_users.ToList());
    }

    public Task<User?> GetUserByIdAsync(Guid id)
    {
        var user = _users.FirstOrDefault(u => u.Id == id);
        return Task.FromResult(user);
    }

    public Task<User?> GetByEmailAsync(string email)
    {
        var user = _users.FirstOrDefault(u => u.Email == email);
        return Task.FromResult(user);
    }

    public Task<User?> GetByUserNameAsync(string username)
    {
        var user = _users.FirstOrDefault(u => u.Username == username);
        return Task.FromResult(user);
    }

    public Task CreateUserAsync(User user)
    {
        _users.Add(user);
        return Task.CompletedTask;
    }

    public Task UpdateUserAsync(User user)
    {
        var existingUser = _users.FirstOrDefault(u => u.Id == user.Id);
        if (existingUser is not null)
        {
            existingUser.Email = user.Email;
            existingUser.Username = user.Username;
            existingUser.PasswordHash = user.PasswordHash;
            existingUser.Role = user.Role;
        }

        return Task.CompletedTask;
    }

    public Task DeleteUserAsync(Guid id)
    {
        _users.RemoveAll(u => u.Id == id);
        return Task.CompletedTask;
    }
}