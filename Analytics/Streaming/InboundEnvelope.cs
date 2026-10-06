using System.Text.Json;

namespace Analytics.Streaming;

public sealed class InboundEnvelope
{
    public Guid EventId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; }
    public int Version { get; set; }
    public JsonElement Payload { get; set; }
}
