using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Events;
using Application.Interfaces;

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
    private readonly IProjectAudience _audience;
    private readonly IRealtimeNotifier _notifier;

    public SignalRNotificationHandler(
        IProjectAudience audience,
        IRealtimeNotifier notifier)
    {
        _audience = audience;
        _notifier = notifier;
    }

     public Task HandleAsync(ProjectCreatedEvent appEvent, CancellationToken cancellationToken = default)
        => NotifyAsync(
            appEvent.ProjectId,
            new RealtimeNotification("project.created", appEvent.ProjectId),
            precomputedAudience: null,
            cancellationToken);
    public Task HandleAsync(ProjectUpdatedEvent appEvent, CancellationToken cancellationToken = default)
        => NotifyAsync(
            appEvent.ProjectId,
            new RealtimeNotification("project.updated", appEvent.ProjectId),
            null,
            cancellationToken);
    public Task HandleAsync(ProjectDeletedEvent appEvent, CancellationToken cancellationToken = default)
        => NotifyAsync(
            appEvent.ProjectId,
            new RealtimeNotification("project.deleted", appEvent.ProjectId),
            appEvent.AudienceUserIds,
            cancellationToken);
    public Task HandleAsync(TaskCreatedEvent appEvent, CancellationToken cancellationToken = default)
        => NotifyAsync(
            appEvent.ProjectId,
            new RealtimeNotification("task.created", appEvent.ProjectId, appEvent.TaskId),
            null,
            cancellationToken);
    public Task HandleAsync(TaskUpdatedEvent appEvent, CancellationToken cancellationToken = default)
        => NotifyAsync(
            appEvent.ProjectId,
            new RealtimeNotification("task.updated", appEvent.ProjectId, appEvent.TaskId),
            null,
            cancellationToken);
    public Task HandleAsync(TaskDeletedEvent appEvent, CancellationToken cancellationToken = default)
        => NotifyAsync(
            appEvent.ProjectId,
            new RealtimeNotification("task.deleted", appEvent.ProjectId, appEvent.TaskId),
            appEvent.AudienceUserIds,
            cancellationToken);
    public Task HandleAsync(CommentAddedEvent appEvent, CancellationToken cancellationToken = default)
        => NotifyAsync(
            appEvent.ProjectId,
            new RealtimeNotification("comment.added", appEvent.ProjectId, appEvent.TaskId, appEvent.CommentId),
            null,
            cancellationToken);
    public Task HandleAsync(CommentUpdatedEvent appEvent, CancellationToken cancellationToken = default)
        => NotifyAsync(
            appEvent.ProjectId,
            new RealtimeNotification("comment.updated", appEvent.ProjectId, appEvent.TaskId, appEvent.CommentId),
            null,
            cancellationToken);
    public Task HandleAsync(CommentDeletedEvent appEvent, CancellationToken cancellationToken = default)
        => NotifyAsync(
            appEvent.ProjectId,
            new RealtimeNotification("comment.deleted", appEvent.ProjectId, appEvent.TaskId, appEvent.CommentId),
            null,
            cancellationToken);
    private async Task NotifyAsync(
        Guid projectId,
        RealtimeNotification notification,
        IReadOnlyList<Guid>? precomputedAudience,
        CancellationToken cancellationToken)
    {
        var userIds = precomputedAudience
            ?? await _audience.GetUserIdsAsync(projectId);
        await _notifier.NotifyUsersAsync(userIds, notification, cancellationToken);
    }
}

