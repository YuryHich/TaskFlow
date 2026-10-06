namespace Application.Events;

public sealed record ProjectCreatedEvent(
    Guid EventId,
    DateTime OccurredAt,
    Guid ProjectId,
    Guid OwnerId,
    Guid ActorUserId,
    string Name,
    IReadOnlyList<Guid> AudienceUserIds) : IAppEvent;

public sealed record ProjectUpdatedEvent(
    Guid EventId,
    DateTime OccurredAt,
    Guid ProjectId,
    Guid ActorUserId,
    string Name,
    IReadOnlyList<Guid> AudienceUserIds) : IAppEvent;

public sealed record ProjectDeletedEvent(
    Guid EventId,
    DateTime OccurredAt,
    Guid ProjectId,
    Guid ActorUserId,
    IReadOnlyList<Guid> AudienceUserIds) : IAppEvent;
