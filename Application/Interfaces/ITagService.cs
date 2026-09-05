using Application.DTOs;

namespace Application.Interfaces;

public interface ITagService
{
    Task<IEnumerable<TagDto>> GetTagsAsync();
    Task<TagDto?> GetTagByIdAsync(Guid id);
    Task<TagDto> CreateTagAsync(CreateTagDto tag);
    Task UpdateTagAsync(Guid id, UpdateTagDto tag);
    Task DeleteTagAsync(Guid id);
}