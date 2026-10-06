using Domain.Models;

namespace Application.Events;

public sealed record TaskCreatedEvent(
    Guid EventId,
    DateTime OccurredAt,
    Guid TaskId,
    Guid ProjectId,
    Guid ActorUserId,
    string Title,
    IReadOnlyList<Guid> AssigneeIds,
    TaskState Status,
    IReadOnlyList<Guid> AudienceUserIds) : IAppEvent;

public sealed record TaskUpdatedEvent(
    Guid EventId,
    DateTime OccurredAt,
    Guid TaskId,
    Guid ProjectId,
    Guid ActorUserId,
    string Title,
    TaskState Status,
    IReadOnlyList<Guid> AudienceUserIds) : IAppEvent;

public sealed record TaskDeletedEvent(
    Guid EventId,
    DateTime OccurredAt,
    Guid TaskId,
    Guid ProjectId,
    Guid ActorUserId,
    IReadOnlyList<Guid> AudienceUserIds) : IAppEvent;
