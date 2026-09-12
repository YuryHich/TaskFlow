using Application.DTOs;
using Application.Events;
using Application.Interfaces;
using Domain.Models;
using Domain.Repositories;
using Mapster;
using Application.Caching;
using Microsoft.Extensions.Options;

namespace Application.Services;

public class TagService : ITagService
{
    private readonly ITagRepository _tagRepository;
    private readonly IAppEventPublisher _appEventPublisher;
    private readonly ICacheService _cache;
    private readonly IOptions<CacheOptions> _cacheOptions;
    public TagService(ITagRepository tagRepository, IAppEventPublisher appEventPublisher, ICacheService cache, IOptions<CacheOptions> cacheOptions)
    {
        _tagRepository = tagRepository;
        _appEventPublisher = appEventPublisher;
        _cache = cache;
        _cacheOptions = cacheOptions;
    }

    public async Task<IEnumerable<TagDto>> GetTagsAsync()
    {
        var cached = await _cache.GetAsync<List<TagDto>>(CacheKeys.TagsAll);
        if (cached is not null)
            return cached;
        var tags = (await _tagRepository.GetTagsAsync()).Adapt<List<TagDto>>();
        await _cache.SetAsync(
            CacheKeys.TagsAll,
            tags,
            TimeSpan.FromMinutes(_cacheOptions.Value.TagsMinutes));
        return tags;
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
        await _appEventPublisher.PublishAsync(new TagCatalogChangedEvent());
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
        await _appEventPublisher.PublishAsync(new TagCatalogChangedEvent());
    }

    public async Task DeleteTagAsync(Guid id)
    {
        await _tagRepository.DeleteTagAsync(id);
        await _appEventPublisher.PublishAsync(new TagCatalogChangedEvent());
    }
}