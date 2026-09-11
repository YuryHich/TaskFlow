namespace Application.Events;

public sealed record TaskCreatedEvent(Guid TaskId, Guid ProjectId) : IAppEvent;

public sealed record TaskUpdatedEvent(Guid TaskId, Guid ProjectId) : IAppEvent;

public sealed record TaskDeletedEvent(Guid TaskId, Guid ProjectId) : IAppEvent;
