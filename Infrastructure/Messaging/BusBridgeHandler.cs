using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Messaging;

public sealed class BusBridgeHandler : IAppEventHandler<TaskCreatedEvent>
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<BusBridgeHandler> _logger;

    public BusBridgeHandler(IPublishEndpoint publishEndpoint, ILogger<BusBridgeHandler> logger)
    {
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task HandleAsync(TaskCreatedEvent appEvent, CancellationToken cancellationToken)
    {
        try
        {
            await _publishEndpoint.Publish(appEvent, cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to publish {EventType} {EventId} to the bus (dual-write)",
                nameof(TaskCreatedEvent),
                appEvent.EventId);
        }
    }
}
