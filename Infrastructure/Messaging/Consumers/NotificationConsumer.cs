using Application.Events;
using Application.Realtime;
using MassTransit;

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
    private readonly SignalRNotificationHandler _handler;

    public NotificationConsumer(SignalRNotificationHandler handler)
    {
        _handler = handler;
    }

    public Task Consume(ConsumeContext<ProjectCreatedEvent> context)
        => _handler.HandleAsync(context.Message, context.CancellationToken);

    public Task Consume(ConsumeContext<ProjectUpdatedEvent> context)
        => _handler.HandleAsync(context.Message, context.CancellationToken);

    public Task Consume(ConsumeContext<ProjectDeletedEvent> context)
        => _handler.HandleAsync(context.Message, context.CancellationToken);

    public Task Consume(ConsumeContext<TaskCreatedEvent> context)
        => _handler.HandleAsync(context.Message, context.CancellationToken);

    public Task Consume(ConsumeContext<TaskUpdatedEvent> context)
        => _handler.HandleAsync(context.Message, context.CancellationToken);

    public Task Consume(ConsumeContext<TaskDeletedEvent> context)
        => _handler.HandleAsync(context.Message, context.CancellationToken);

    public Task Consume(ConsumeContext<CommentAddedEvent> context)
        => _handler.HandleAsync(context.Message, context.CancellationToken);

    public Task Consume(ConsumeContext<CommentUpdatedEvent> context)
        => _handler.HandleAsync(context.Message, context.CancellationToken);

    public Task Consume(ConsumeContext<CommentDeletedEvent> context)
        => _handler.HandleAsync(context.Message, context.CancellationToken);
}
