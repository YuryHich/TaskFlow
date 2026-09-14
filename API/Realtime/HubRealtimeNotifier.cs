using API.Hubs;
using Application.Realtime;
using Microsoft.AspNetCore.SignalR;

namespace API.Realtime;

public sealed class HubRealtimeNotifier : IRealtimeNotifier
{
    private readonly IHubContext<NotificationHub> _hub;

    public HubRealtimeNotifier(IHubContext<NotificationHub> hub)
    {
        _hub = hub;
    }

    public Task NotifyUsersAsync(
        IReadOnlyList<Guid> userIds,
        RealtimeNotification notification,
        CancellationToken cancellationToken = default)
    {
        if (userIds.Count == 0)
            return Task.CompletedTask;

        var ids = userIds
            .Distinct()
            .Select(id => id.ToString())
            .ToList();

        return _hub.Clients.Users(ids).SendAsync("Notify", notification, cancellationToken);
    }
}