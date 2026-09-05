using Application.DTOs;
using Application.Interfaces;
using Domain.Models;
using Domain.Repositories;
using Mapster;

namespace Application.Services;

public class TagService : ITagService
{
    private readonly ITagRepository _tagRepository;

    public TagService(ITagRepository tagRepository)
    {
        _tagRepository = tagRepository;
    }

    public async Task<IEnumerable<TagDto>> GetTagsAsync()
    {
        var tags = await _tagRepository.GetTagsAsync();
        return tags.Adapt<IEnumerable<TagDto>>();
    }

    public async Task<TagDto?> GetTagByIdAsync(Guid id)
    {
        var tag = await _tagRepository.GetTagByIdAsync(id);
        return tag?.Adapt<TagDto>();
    }

    public async Task<TagDto> CreateTagAsync(CreateTagDto tag)
    {
        var tagEntity = tag.Adapt<Tag>();
        tagEntity.Id = Guid.NewGuid();

        await _tagRepository.CreateTagAsync(tagEntity);
        return tagEntity.Adapt<TagDto>();
    }

    public async Task UpdateTagAsync(Guid id, UpdateTagDto tag)
    {
        var tagEntity = await _tagRepository.GetTagByIdAsync(id);
        if (tagEntity is null)
        {
            return;
        }

        tag.Adapt(tagEntity);
        await _tagRepository.UpdateTagAsync(tagEntity);
    }

    public async Task DeleteTagAsync(Guid id)
    {
        await _tagRepository.DeleteTagAsync(id);
    }
}