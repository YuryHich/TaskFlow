using System.Collections.Generic;
namespace Application.Events;

public sealed record TaskCreatedEvent(
    Guid EventId,
    DateTime OccurredAt,
    Guid TaskId,
    Guid ProjectId,
    Guid ActorUserId,
    string Title,
    IReadOnlyList<Guid> AssigneeIds) : IAppEvent;

public sealed record TaskUpdatedEvent(Guid TaskId, Guid ProjectId) : IAppEvent;

public sealed record TaskDeletedEvent(Guid TaskId, Guid ProjectId, IReadOnlyList<Guid> AudienceUserIds) : IAppEvent;
