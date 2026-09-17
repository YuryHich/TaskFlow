using System.Text.Json;
using Application.Events;
using Domain.Models;
using Domain.Repositories;
using MassTransit;

namespace Infrastructure.Messaging.Consumers;

public sealed class AuditConsumer :
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
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IAuditLogRepository _auditLogs;

    public AuditConsumer(IAuditLogRepository auditLogs)
    {
        _auditLogs = auditLogs;
    }

    public Task Consume(ConsumeContext<ProjectCreatedEvent> context)
        => Write(context, "Project", context.Message.ProjectId, context.Message.ProjectId, context.Message.EventId, context.Message.OccurredAt, context.Message.ActorUserId);

    public Task Consume(ConsumeContext<ProjectUpdatedEvent> context)
        => Write(context, "Project", context.Message.ProjectId, context.Message.ProjectId, context.Message.EventId, context.Message.OccurredAt, context.Message.ActorUserId);

    public Task Consume(ConsumeContext<ProjectDeletedEvent> context)
        => Write(context, "Project", context.Message.ProjectId, context.Message.ProjectId, context.Message.EventId, context.Message.OccurredAt, context.Message.ActorUserId);

    public Task Consume(ConsumeContext<TaskCreatedEvent> context)
        => Write(context, "Task", context.Message.TaskId, context.Message.ProjectId, context.Message.EventId, context.Message.OccurredAt, context.Message.ActorUserId);

    public Task Consume(ConsumeContext<TaskUpdatedEvent> context)
        => Write(context, "Task", context.Message.TaskId, context.Message.ProjectId, context.Message.EventId, context.Message.OccurredAt, context.Message.ActorUserId);

    public Task Consume(ConsumeContext<TaskDeletedEvent> context)
        => Write(context, "Task", context.Message.TaskId, context.Message.ProjectId, context.Message.EventId, context.Message.OccurredAt, context.Message.ActorUserId);

    public Task Consume(ConsumeContext<CommentAddedEvent> context)
        => Write(context, "Comment", context.Message.CommentId, context.Message.ProjectId, context.Message.EventId, context.Message.OccurredAt, context.Message.ActorUserId);

    public Task Consume(ConsumeContext<CommentUpdatedEvent> context)
        => Write(context, "Comment", context.Message.CommentId, context.Message.ProjectId, context.Message.EventId, context.Message.OccurredAt, context.Message.ActorUserId);

    public Task Consume(ConsumeContext<CommentDeletedEvent> context)
        => Write(context, "Comment", context.Message.CommentId, context.Message.ProjectId, context.Message.EventId, context.Message.OccurredAt, context.Message.ActorUserId);

    private Task Write<T>(
        ConsumeContext<T> context,
        string entityType,
        Guid entityId,
        Guid projectId,
        Guid eventId,
        DateTime occurredAt,
        Guid actorUserId)
        where T : class
    {
        var log = new AuditLog
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            EventType = typeof(T).Name,
            EntityType = entityType,
            EntityId = entityId,
            ProjectId = projectId,
            ActorUserId = actorUserId,
            OccurredAt = occurredAt,
            Payload = JsonSerializer.Serialize(context.Message, JsonOptions)
        };
        return _auditLogs.TryAddAsync(log, context.CancellationToken);
    }
}
