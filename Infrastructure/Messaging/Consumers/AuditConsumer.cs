using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MassTransit;
using Application.Events;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Messaging.Consumers;

public sealed class AuditConsumer : IConsumer<TaskCreatedEvent>
{
    private readonly ILogger<AuditConsumer> _logger;
    public AuditConsumer(ILogger<AuditConsumer> logger)
    {
        _logger = logger;
    }
    public Task Consume(ConsumeContext<TaskCreatedEvent> context)
    {
        _logger.LogInformation(
            "AuditConsumer stub. Event TaskId={TaskId} MessageId={MessageId}",
            context.Message.TaskId,
            context.MessageId);
        return Task.CompletedTask;
    }
}
