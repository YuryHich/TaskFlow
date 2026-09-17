using Application.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Messaging;

public sealed class BusBridgeHandler :
    IAppEventHandler<ProjectCreatedEvent>,
    IAppEventHandler<ProjectUpdatedEvent>,
    IAppEventHandler<ProjectDeletedEvent>,
    IAppEventHandler<TaskCreatedEvent>,
    IAppEventHandler<TaskUpdatedEvent>,
    IAppEventHandler<TaskDeletedEvent>,
    IAppEventHandler<CommentAddedEvent>,
    IAppEventHandler<CommentUpdatedEvent>,
    IAppEventHandler<CommentDeletedEvent>
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<BusBridgeHandler> _logger;

    public BusBridgeHandler(IPublishEndpoint publishEndpoint, ILogger<BusBridgeHandler> logger)
    {
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public Task HandleAsync(ProjectCreatedEvent appEvent, CancellationToken cancellationToken = default)
        => PublishSafe(appEvent, appEvent.EventId, cancellationToken);

    public Task HandleAsync(ProjectUpdatedEvent appEvent, CancellationToken cancellationToken = default)
        => PublishSafe(appEvent, appEvent.EventId, cancellationToken);

    public Task HandleAsync(ProjectDeletedEvent appEvent, CancellationToken cancellationToken = default)
        => PublishSafe(appEvent, appEvent.EventId, cancellationToken);

    public Task HandleAsync(TaskCreatedEvent appEvent, CancellationToken cancellationToken = default)
        => PublishSafe(appEvent, appEvent.EventId, cancellationToken);

    public Task HandleAsync(TaskUpdatedEvent appEvent, CancellationToken cancellationToken = default)
        => PublishSafe(appEvent, appEvent.EventId, cancellationToken);

    public Task HandleAsync(TaskDeletedEvent appEvent, CancellationToken cancellationToken = default)
        => PublishSafe(appEvent, appEvent.EventId, cancellationToken);

    public Task HandleAsync(CommentAddedEvent appEvent, CancellationToken cancellationToken = default)
        => PublishSafe(appEvent, appEvent.EventId, cancellationToken);

    public Task HandleAsync(CommentUpdatedEvent appEvent, CancellationToken cancellationToken = default)
        => PublishSafe(appEvent, appEvent.EventId, cancellationToken);

    public Task HandleAsync(CommentDeletedEvent appEvent, CancellationToken cancellationToken = default)
        => PublishSafe(appEvent, appEvent.EventId, cancellationToken);

    private async Task PublishSafe<TEvent>(TEvent appEvent, Guid eventId, CancellationToken cancellationToken)
        where TEvent : class
    {
        try
        {
            await _publishEndpoint.Publish(appEvent, cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to publish {EventType} {EventId} to the bus (dual-write)",
                typeof(TEvent).Name,
                eventId);
        }
    }
}
