using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Events;
using Domain.Models;

namespace Application.Caching;

public sealed class CacheInvalidationHandler :
    IAppEventHandler<ProjectCreatedEvent>,
    IAppEventHandler<ProjectUpdatedEvent>,
    IAppEventHandler<ProjectDeletedEvent>,
    IAppEventHandler<TaskCreatedEvent>,
    IAppEventHandler<TaskUpdatedEvent>,
    IAppEventHandler<TaskDeletedEvent>,
    IAppEventHandler<TagCatalogChangedEvent>
{
    private readonly ICacheService _cache;

    public CacheInvalidationHandler(ICacheService cache)
    {
        _cache = cache;
    }

    public Task HandleAsync(ProjectCreatedEvent appEvent, CancellationToken cancellationToken = default)
        => _cache.RemoveAsync(CacheKeys.Project(appEvent.ProjectId), cancellationToken);
    public Task HandleAsync(ProjectUpdatedEvent appEvent, CancellationToken cancellationToken = default)
        => _cache.RemoveAsync(CacheKeys.Project(appEvent.ProjectId), cancellationToken);
    public Task HandleAsync(ProjectDeletedEvent appEvent, CancellationToken cancellationToken = default)
        => _cache.RemoveAsync(CacheKeys.Project(appEvent.ProjectId), cancellationToken);
    public Task HandleAsync(TaskCreatedEvent appEvent, CancellationToken cancellationToken = default)
        => _cache.RemoveAsync(CacheKeys.Task(appEvent.TaskId), cancellationToken);
    public Task HandleAsync(TaskUpdatedEvent appEvent, CancellationToken cancellationToken = default)
        => _cache.RemoveAsync(CacheKeys.Task(appEvent.TaskId), cancellationToken);
    public Task HandleAsync(TaskDeletedEvent appEvent, CancellationToken cancellationToken = default)
        => _cache.RemoveAsync(CacheKeys.Task(appEvent.TaskId), cancellationToken);
    public Task HandleAsync(TagCatalogChangedEvent appEvent, CancellationToken cancellationToken = default)
        => _cache.RemoveAsync(CacheKeys.TagsAll, cancellationToken);
}
