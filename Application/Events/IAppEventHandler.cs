using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Events;

public interface IAppEventHandler<in TEvent> where TEvent : IAppEvent
{
    Task HandleAsync(TEvent appEvent, CancellationToken cancellationToken = default);
}
