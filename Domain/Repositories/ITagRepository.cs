using Domain.Models;

namespace Domain.Repositories
{
    public interface ITagRepository
    {
        public Task<IEnumerable<Tag>> GetTagsAsync();
        public Task<Tag?> GetTagByIdAsync(Guid id);
        public Task CreateTagAsync(Tag tag);
        public Task UpdateTagAsync(Tag tag);
        public Task DeleteTagAsync(Guid id);
    }
}