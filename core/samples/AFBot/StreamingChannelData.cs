
using System.Text.Json.Serialization;
using Microsoft.Bot.Core.Schema;

namespace AFBot
{
    public static class StreamType
    {
        public const string Informative = "informative";
        public const string Streaming = "streaming";
        public const string Final = "final";
    }

    public class StreamingChannelData : ChannelData
    {
        [JsonPropertyName(  "streamId")]
        public string StreamId { get; set; } = string.Empty;

        [JsonPropertyName("streamType")]
        public string? StreamType { get; set; }

        [JsonPropertyName("streamSequence")]
        public int StreamSequence { get; set; }
    }
}
