namespace Microsoft.Bot.Core.Schema;

public class ChannelData()
{
    [JsonPropertyName("clientActivityID")]
    public string? ClientActivityId { get; set; }

    [JsonExtensionData]
    public ExtendedPropertiesDictionary Properties { get; set; } = new();
}
