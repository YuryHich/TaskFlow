using Domain.Models;
using Domain.Repositories;

namespace Infrastructure.Repositories;

public class InMemoryTagRepository : ITagRepository
{
    private readonly List<Tag> _tags = [];

    public Task<IEnumerable<Tag>> GetTagsAsync()
    {
        return Task.FromResult<IEnumerable<Tag>>(_tags.ToList());
    }

    public Task<Tag?> GetTagByIdAsync(Guid id)
    {
        var tag = _tags.FirstOrDefault(t => t.Id == id);
        return Task.FromResult(tag);
    }

    public Task CreateTagAsync(Tag tag)
    {
        _tags.Add(tag);
        return Task.CompletedTask;
    }

    public Task UpdateTagAsync(Tag tag)
    {
        var existingTag = _tags.FirstOrDefault(t => t.Id == tag.Id);
        if (existingTag is not null)
        {
            existingTag.Name = tag.Name;
        }

        return Task.CompletedTask;
    }

    public Task DeleteTagAsync(Guid id)
    {
        _tags.RemoveAll(t => t.Id == id);
        return Task.CompletedTask;
    }
}