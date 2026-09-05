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
            existingUser.FullName = user.FullName;
        }

        return Task.CompletedTask;
    }

    public Task DeleteUserAsync(Guid id)
    {
        _users.RemoveAll(u => u.Id == id);
        return Task.CompletedTask;
    }
}