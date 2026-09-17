namespace Application.Events;

public sealed record CommentAddedEvent(
    Guid EventId,
    DateTime OccurredAt,
    Guid CommentId,
    Guid TaskId,
    Guid ProjectId,
    Guid ActorUserId) : IAppEvent;

public sealed record CommentUpdatedEvent(
    Guid EventId,
    DateTime OccurredAt,
    Guid CommentId,
    Guid TaskId,
    Guid ProjectId,
    Guid ActorUserId) : IAppEvent;

public sealed record CommentDeletedEvent(
    Guid EventId,
    DateTime OccurredAt,
    Guid CommentId,
    Guid TaskId,
    Guid ProjectId,
    Guid ActorUserId) : IAppEvent;
