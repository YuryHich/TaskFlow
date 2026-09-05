using Domain.Models;

namespace Domain.Repositories
{
    public interface IUserRepository
    {
        public Task<IEnumerable<User>> GetUsersAsync();
        public Task<User?> GetUserByIdAsync(Guid id);
        public Task CreateUserAsync(User user);
        public Task UpdateUserAsync(User user);
        public Task DeleteUserAsync(Guid id);
    }
}