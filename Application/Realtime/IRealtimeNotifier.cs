using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Realtime;

public interface IRealtimeNotifier
{
    Task NotifyUsersAsync(
        IReadOnlyList<Guid> userIds,
        RealtimeNotification notification,
        CancellationToken cancellationToken = default
    );
}