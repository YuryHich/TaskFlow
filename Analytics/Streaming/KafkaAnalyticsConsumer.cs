using Confluent.Kafka;
using Microsoft.Extensions.Options;

namespace Analytics.Streaming;

public sealed class KafkaAnalyticsConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly AnalyticsKafkaOptions _options;
    private readonly ILogger<KafkaAnalyticsConsumer> _logger;

    public KafkaAnalyticsConsumer(
        IServiceScopeFactory scopes,
        IOptions<AnalyticsKafkaOptions> options,
        ILogger<KafkaAnalyticsConsumer> logger)
    {
        _scopes = scopes;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ConsumeAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Kafka analytics consumer stopped; retrying");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task ConsumeAsync(CancellationToken stoppingToken)
    {
        using var consumer = new ConsumerBuilder<string, string>(new ConsumerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            GroupId = _options.ConsumerGroup,
            EnableAutoCommit = false,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            AllowAutoCreateTopics = false
        }).SetPartitionsAssignedHandler((assignedConsumer, partitions) =>
        {
            if (!_options.ResetToBeginning)
                return;

            foreach (var partition in partitions)
                assignedConsumer.Seek(new TopicPartitionOffset(partition, Offset.Beginning));
        }).Build();

        consumer.Subscribe(_options.Topic);
        _logger.LogInformation(
            "Analytics listening on {Topic} as {Group} (reset={Reset})",
            _options.Topic,
            _options.ConsumerGroup,
            _options.ResetToBeginning);

        while (!stoppingToken.IsCancellationRequested)
        {
            ConsumeResult<string, string>? result;
            try
            {
                result = consumer.Consume(stoppingToken);
            }
            catch (ConsumeException exception)
            {
                _logger.LogError(exception, "Kafka consume failed");
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                continue;
            }

            if (result is null)
                continue;

            try
            {
                await using var scope = _scopes.CreateAsyncScope();
                var reducer = scope.ServiceProvider.GetRequiredService<EventReducer>();
                await reducer.ApplyAsync(result.Message.Value, stoppingToken);
                consumer.Commit(result);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                _logger.LogError(exception, "Failed to apply offset {Offset}; will retry", result.Offset);
                consumer.Seek(new TopicPartitionOffset(result.TopicPartition, result.Offset));
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
        }

        consumer.Close();
    }
}
