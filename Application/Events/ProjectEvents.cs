namespace Application.Events;

public sealed record ProjectCreatedEvent(Guid ProjectId, Guid OwnerId) : IAppEvent;

public sealed record ProjectUpdatedEvent(Guid ProjectId) : IAppEvent;

public sealed record ProjectDeletedEvent(Guid ProjectId, IReadOnlyList<Guid>  AudienceUserIds) : IAppEvent;
