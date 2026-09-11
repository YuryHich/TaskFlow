using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Application.Events;

public class AppEventPublisher : IAppEventPublisher
{
    private readonly IServiceProvider _services;
    private readonly ILogger<AppEventPublisher> _logger;

    public AppEventPublisher(IServiceProvider services, ILogger<AppEventPublisher> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task PublishAsync<TEvent>(TEvent appEvent, CancellationToken cancellationToken = default) where TEvent : IAppEvent
    {
        var handlers = _services.GetServices<IAppEventHandler<TEvent>>();
        foreach (var handler in handlers)
        {
            try
            {
                await handler.HandleAsync(appEvent, cancellationToken);
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "App event handler {Handler} failed for {EventType}",
                    handler.GetType().Name,
                    typeof(TEvent).Name
                );
            }
        }
    }
}
