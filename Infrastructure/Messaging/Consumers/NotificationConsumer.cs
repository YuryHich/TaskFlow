using Application.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Messaging.Consumers;

public sealed class NotificationConsumer :
    IConsumer<ProjectCreatedEvent>,
    IConsumer<ProjectUpdatedEvent>,
    IConsumer<ProjectDeletedEvent>,
    IConsumer<TaskCreatedEvent>,
    IConsumer<TaskUpdatedEvent>,
    IConsumer<TaskDeletedEvent>,
    IConsumer<CommentAddedEvent>,
    IConsumer<CommentUpdatedEvent>,
    IConsumer<CommentDeletedEvent>
{
    private readonly ILogger<NotificationConsumer> _logger;

    public NotificationConsumer(ILogger<NotificationConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<ProjectCreatedEvent> context)
        => Log(context, context.Message.EventId, context.Message.ProjectId);

    public Task Consume(ConsumeContext<ProjectUpdatedEvent> context)
        => Log(context, context.Message.EventId, context.Message.ProjectId);

    public Task Consume(ConsumeContext<ProjectDeletedEvent> context)
        => Log(context, context.Message.EventId, context.Message.ProjectId);

    public Task Consume(ConsumeContext<TaskCreatedEvent> context)
        => Log(context, context.Message.EventId, context.Message.ProjectId, context.Message.TaskId);

    public Task Consume(ConsumeContext<TaskUpdatedEvent> context)
        => Log(context, context.Message.EventId, context.Message.ProjectId, context.Message.TaskId);

    public Task Consume(ConsumeContext<TaskDeletedEvent> context)
        => Log(context, context.Message.EventId, context.Message.ProjectId, context.Message.TaskId);

    public Task Consume(ConsumeContext<CommentAddedEvent> context)
        => Log(context, context.Message.EventId, context.Message.ProjectId, context.Message.TaskId, context.Message.CommentId);

    public Task Consume(ConsumeContext<CommentUpdatedEvent> context)
        => Log(context, context.Message.EventId, context.Message.ProjectId, context.Message.TaskId, context.Message.CommentId);

    public Task Consume(ConsumeContext<CommentDeletedEvent> context)
        => Log(context, context.Message.EventId, context.Message.ProjectId, context.Message.TaskId, context.Message.CommentId);

    private Task Log<T>(
        ConsumeContext<T> context,
        Guid eventId,
        Guid projectId,
        Guid? taskId = null,
        Guid? commentId = null)
        where T : class
    {
        _logger.LogInformation(
            "NotificationConsumer stub {EventType} EventId={EventId} ProjectId={ProjectId} TaskId={TaskId} CommentId={CommentId} MessageId={MessageId}",
            typeof(T).Name,
            eventId,
            projectId,
            taskId,
            commentId,
            context.MessageId);
        return Task.CompletedTask;
    }
}
