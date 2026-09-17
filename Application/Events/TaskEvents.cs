namespace Application.Events;

public sealed record TaskCreatedEvent(
    Guid EventId,
    DateTime OccurredAt,
    Guid TaskId,
    Guid ProjectId,
    Guid ActorUserId,
    string Title,
    IReadOnlyList<Guid> AssigneeIds) : IAppEvent;

public sealed record TaskUpdatedEvent(
    Guid EventId,
    DateTime OccurredAt,
    Guid TaskId,
    Guid ProjectId,
    Guid ActorUserId) : IAppEvent;

public sealed record TaskDeletedEvent(
    Guid EventId,
    DateTime OccurredAt,
    Guid TaskId,
    Guid ProjectId,
    Guid ActorUserId,
    IReadOnlyList<Guid> AudienceUserIds) : IAppEvent;
