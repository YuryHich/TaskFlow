using Application.Realtime;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;

namespace API.Tests.Infrastructure;

public static class HubTestHelper
{
    public static HubConnection CreateConnection(
        TaskFlowApiFixture fixture,
        string? accessToken,
        bool tokenInQuery = false)
    {
        var hubUri = new Uri(fixture.Server.BaseAddress!, "/hubs/notifications");
        if (tokenInQuery && !string.IsNullOrEmpty(accessToken))
        {
            var builder = new UriBuilder(hubUri)
            {
                Query = $"access_token={Uri.EscapeDataString(accessToken)}"
            };
            hubUri = builder.Uri;
        }

        return new HubConnectionBuilder()
            .WithUrl(hubUri, options =>
            {
                options.HttpMessageHandlerFactory = _ => fixture.Server.CreateHandler();
                options.Transports = HttpTransportType.LongPolling;
                if (!tokenInQuery && !string.IsNullOrEmpty(accessToken))
                    options.AccessTokenProvider = () => Task.FromResult<string?>(accessToken);
            })
            .Build();
    }

    public static Task<RealtimeNotification> WaitNotifyAsync(
        HubConnection connection,
        TimeSpan? timeout = null)
    {
        var tcs = new TaskCompletionSource<RealtimeNotification>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        connection.On<RealtimeNotification>("Notify", n => tcs.TrySetResult(n));
        return tcs.Task.WaitAsync(timeout ?? TimeSpan.FromSeconds(5));
    }
}