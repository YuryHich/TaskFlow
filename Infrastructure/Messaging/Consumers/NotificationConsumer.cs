using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.Extensions.Logging;
using Application.Events;

namespace Infrastructure.Messaging.Consumers;

public sealed class NotificationConsumer : IConsumer<TaskCreatedEvent>
{
    private readonly ILogger<NotificationConsumer> _logger;
    public NotificationConsumer(ILogger<NotificationConsumer> logger)
    {
        _logger = logger;
    }
    public Task Consume(ConsumeContext<TaskCreatedEvent> context)
    {
        _logger.LogInformation(
            "NotificationConsumer stub. Event TaskId={TaskId} MessageId={MessageId}",
            context.Message.TaskId,
            context.MessageId);
        return Task.CompletedTask;
    }
}
