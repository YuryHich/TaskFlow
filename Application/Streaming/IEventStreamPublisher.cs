namespace Application.Streaming;

public interface IEventStreamPublisher
{
    Task PublishAsync(string key, StreamEnvelope envelope, CancellationToken cancellationToken = default);
}
