namespace Microsoft.Bot.Core.Schema;

/// <summary>
/// Represents a conversation, including its unique identifier and associated extended properties.
/// </summary>
public class Conversation()
{
    /// <summary>
    /// Gets or sets the unique identifier for the object.
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// Gets the extension data dictionary for storing additional properties not defined in the schema.
    /// </summary>
    [JsonExtensionData]
    public ExtendedPropertiesDictionary Properties { get; init; } = [];
}