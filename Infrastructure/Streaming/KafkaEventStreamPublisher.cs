using System.Text.Json;
using Application.Streaming;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Streaming;

public sealed class KafkaEventStreamPublisher : IEventStreamPublisher, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly string _topic;
    private readonly ILogger<KafkaEventStreamPublisher> _logger;

    public KafkaEventStreamPublisher(IOptions<KafkaOptions> options, ILogger<KafkaEventStreamPublisher> logger)
    {
        _topic = options.Value.Topic;
        _logger = logger;
        _producer = new ProducerBuilder<string, string>(new ProducerConfig
        {
            BootstrapServers = options.Value.BootstrapServers,
            Acks = Acks.All,
            MessageTimeoutMs = 2000,
            SocketTimeoutMs = 2000,
            LingerMs = 5
        }).Build();
    }

    public async Task PublishAsync(string key, StreamEnvelope envelope, CancellationToken cancellationToken = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(envelope, StreamJson.Options);
            await _producer.ProduceAsync(_topic, new Message<string, string>
            {
                Key = key,
                Value = json
            }, cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to publish {EventType} {EventId} to Kafka (dual-write)",
                envelope.EventType,
                envelope.EventId);
        }
    }

    public void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(2));
        _producer.Dispose();
    }
}
