using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Notification.Hubs;

[Authorize]
public sealed class NotificationHub : Hub;
