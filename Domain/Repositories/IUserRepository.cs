using Domain.Models;

namespace Domain.Repositories
{
    public interface IUserRepository
    {
        public Task<IEnumerable<User>> GetUsersAsync();
        public Task<User?> GetUserByIdAsync(Guid id);
        public Task<User?> GetByEmailAsync(string email);
        public Task<User?> GetByUserNameAsync(string username);
        public Task CreateUserAsync(User user);
        public Task UpdateUserAsync(User user);
        public Task DeleteUserAsync(Guid id);
        public Task<IReadOnlyList<Guid>> GetUserIdsByRoleAsync(params UserRole[] roles);
    }
}