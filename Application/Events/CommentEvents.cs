namespace Application.Events;

public sealed record CommentAddedEvent(Guid CommentId, Guid TaskId, Guid ProjectId) : IAppEvent;

public sealed record CommentUpdatedEvent(Guid CommentId, Guid TaskId, Guid ProjectId) : IAppEvent;

public sealed record CommentDeletedEvent(Guid CommentId, Guid TaskId, Guid ProjectId) : IAppEvent;
