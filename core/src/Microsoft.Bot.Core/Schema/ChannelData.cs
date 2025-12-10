namespace Microsoft.Bot.Core.Schema;

/// <summary>
/// Represents channel-specific data associated with an activity.
/// </summary>
/// <remarks>
/// This class serves as a container for custom properties that are specific to a particular
/// messaging channel. The properties dictionary allows channels to include additional metadata
/// that is not part of the standard activity schema.
/// </remarks>
public class ChannelData()
{
    /// <summary>
    /// Gets the extension data dictionary for storing channel-specific properties.
    /// </summary>
    [JsonExtensionData]
    public ExtendedPropertiesDictionary Properties { get; init; } = [];
}