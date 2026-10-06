namespace Analytics.Streaming;

public sealed class AnalyticsKafkaOptions
{
    public const string SectionName = "Kafka";

    public string BootstrapServers { get; set; } = "localhost:9092";
    public string Topic { get; set; } = "taskflow.events";
    public string ConsumerGroup { get; set; } = "taskflow-analytics";
    public bool ResetToBeginning { get; set; }
}
