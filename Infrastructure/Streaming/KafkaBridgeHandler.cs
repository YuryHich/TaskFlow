using Application.Events;
using Application.Streaming;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Streaming;

public sealed class KafkaBridgeHandler :
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
    private readonly IEventStreamPublisher _publisher;
    private readonly ILogger<KafkaBridgeHandler> _logger;

    public KafkaBridgeHandler(IEventStreamPublisher publisher, ILogger<KafkaBridgeHandler> logger)
    {
        _publisher = publisher;
        _logger = logger;
    }

    public Task HandleAsync(ProjectCreatedEvent appEvent, CancellationToken cancellationToken = default)
        => Publish(appEvent.ProjectId, StreamEventTypes.ProjectCreated, appEvent, appEvent.EventId, appEvent.OccurredAt, cancellationToken);

    public Task HandleAsync(ProjectUpdatedEvent appEvent, CancellationToken cancellationToken = default)
        => Publish(appEvent.ProjectId, StreamEventTypes.ProjectUpdated, appEvent, appEvent.EventId, appEvent.OccurredAt, cancellationToken);

    public Task HandleAsync(ProjectDeletedEvent appEvent, CancellationToken cancellationToken = default)
        => Publish(appEvent.ProjectId, StreamEventTypes.ProjectDeleted, appEvent, appEvent.EventId, appEvent.OccurredAt, cancellationToken);

    public Task HandleAsync(TaskCreatedEvent appEvent, CancellationToken cancellationToken = default)
        => Publish(appEvent.TaskId, StreamEventTypes.TaskCreated, appEvent, appEvent.EventId, appEvent.OccurredAt, cancellationToken);

    public Task HandleAsync(TaskUpdatedEvent appEvent, CancellationToken cancellationToken = default)
        => Publish(appEvent.TaskId, StreamEventTypes.TaskUpdated, appEvent, appEvent.EventId, appEvent.OccurredAt, cancellationToken);

    public Task HandleAsync(TaskDeletedEvent appEvent, CancellationToken cancellationToken = default)
        => Publish(appEvent.TaskId, StreamEventTypes.TaskDeleted, appEvent, appEvent.EventId, appEvent.OccurredAt, cancellationToken);

    public Task HandleAsync(CommentAddedEvent appEvent, CancellationToken cancellationToken = default)
        => Publish(appEvent.TaskId, StreamEventTypes.CommentAdded, appEvent, appEvent.EventId, appEvent.OccurredAt, cancellationToken);

    public Task HandleAsync(CommentUpdatedEvent appEvent, CancellationToken cancellationToken = default)
        => Publish(appEvent.TaskId, StreamEventTypes.CommentUpdated, appEvent, appEvent.EventId, appEvent.OccurredAt, cancellationToken);

    public Task HandleAsync(CommentDeletedEvent appEvent, CancellationToken cancellationToken = default)
        => Publish(appEvent.TaskId, StreamEventTypes.CommentDeleted, appEvent, appEvent.EventId, appEvent.OccurredAt, cancellationToken);

    private async Task Publish<TEvent>(
        Guid key,
        string eventType,
        TEvent appEvent,
        Guid eventId,
        DateTime occurredAt,
        CancellationToken cancellationToken)
        where TEvent : class
    {
        try
        {
            var envelope = new StreamEnvelope
            {
                EventId = eventId,
                EventType = eventType,
                OccurredAt = occurredAt,
                Version = 1,
                Payload = System.Text.Json.JsonSerializer.SerializeToElement(appEvent, StreamJson.Options)
            };
            await _publisher.PublishAsync(key.ToString(), envelope, cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Kafka bridge failed for {EventType} {EventId}", eventType, eventId);
        }
    }
}
