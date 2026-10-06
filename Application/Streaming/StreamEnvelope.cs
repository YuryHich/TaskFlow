using System.Text.Json;

namespace Application.Streaming;

public sealed class StreamEnvelope
{
    public Guid EventId { get; init; }
    public string EventType { get; init; } = string.Empty;
    public DateTime OccurredAt { get; init; }
    public int Version { get; init; } = 1;
    public JsonElement Payload { get; init; }
}
