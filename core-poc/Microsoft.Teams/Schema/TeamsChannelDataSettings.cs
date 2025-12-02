using Microsoft.Bot.Core.Schema;

using System.Text.Json.Serialization;

namespace Microsoft.Teams.Schema;

public class TeamsChannelDataSettings
{
    [JsonPropertyName("selectedChannel")]
    public required TeamsChannel SelectedChannel { get; set; }
    [JsonExtensionData]
    public ExtendedPropertiesDictionary Properties { get; set; } = [];
}
