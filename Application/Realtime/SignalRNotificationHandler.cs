using Application.Events;

namespace Application.Realtime;

public class SignalRNotificationHandler :
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
    private readonly IRealtimeNotifier _notifier;

    public SignalRNotificationHandler(IRealtimeNotifier notifier)
    {
        _notifier = notifier;
    }

    public Task HandleAsync(ProjectCreatedEvent appEvent, CancellationToken cancellationToken = default)
        => Notify(appEvent.AudienceUserIds, new RealtimeNotification("project.created", appEvent.ProjectId), cancellationToken);

    public Task HandleAsync(ProjectUpdatedEvent appEvent, CancellationToken cancellationToken = default)
        => Notify(appEvent.AudienceUserIds, new RealtimeNotification("project.updated", appEvent.ProjectId), cancellationToken);

    public Task HandleAsync(ProjectDeletedEvent appEvent, CancellationToken cancellationToken = default)
        => Notify(appEvent.AudienceUserIds, new RealtimeNotification("project.deleted", appEvent.ProjectId), cancellationToken);

    public Task HandleAsync(TaskCreatedEvent appEvent, CancellationToken cancellationToken = default)
        => Notify(appEvent.AudienceUserIds, new RealtimeNotification("task.created", appEvent.ProjectId, appEvent.TaskId), cancellationToken);

    public Task HandleAsync(TaskUpdatedEvent appEvent, CancellationToken cancellationToken = default)
        => Notify(appEvent.AudienceUserIds, new RealtimeNotification("task.updated", appEvent.ProjectId, appEvent.TaskId), cancellationToken);

    public Task HandleAsync(TaskDeletedEvent appEvent, CancellationToken cancellationToken = default)
        => Notify(appEvent.AudienceUserIds, new RealtimeNotification("task.deleted", appEvent.ProjectId, appEvent.TaskId), cancellationToken);

    public Task HandleAsync(CommentAddedEvent appEvent, CancellationToken cancellationToken = default)
        => Notify(appEvent.AudienceUserIds, new RealtimeNotification("comment.added", appEvent.ProjectId, appEvent.TaskId, appEvent.CommentId), cancellationToken);

    public Task HandleAsync(CommentUpdatedEvent appEvent, CancellationToken cancellationToken = default)
        => Notify(appEvent.AudienceUserIds, new RealtimeNotification("comment.updated", appEvent.ProjectId, appEvent.TaskId, appEvent.CommentId), cancellationToken);

    public Task HandleAsync(CommentDeletedEvent appEvent, CancellationToken cancellationToken = default)
        => Notify(appEvent.AudienceUserIds, new RealtimeNotification("comment.deleted", appEvent.ProjectId, appEvent.TaskId, appEvent.CommentId), cancellationToken);

    private Task Notify(
        IReadOnlyList<Guid> audienceUserIds,
        RealtimeNotification notification,
        CancellationToken cancellationToken)
        => _notifier.NotifyUsersAsync(audienceUserIds, notification, cancellationToken);
}
