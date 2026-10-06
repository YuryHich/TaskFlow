using System.Text.Json;
using System.Text.Json.Serialization;

namespace Infrastructure.Streaming;

internal static class StreamJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };
}
