using Application.Streaming;

namespace Infrastructure.Streaming;

public sealed class NoOpEventStreamPublisher : IEventStreamPublisher
{
    public Task PublishAsync(string key, StreamEnvelope envelope, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
