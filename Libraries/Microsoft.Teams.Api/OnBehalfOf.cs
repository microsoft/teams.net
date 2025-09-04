using System.Text.Json.Serialization;

namespace Microsoft.Teams.Api;

/// <summary>
/// Represents information about a user on behalf of whom an action is performed.
/// </summary>
public class OnBehalfOf
{
    /// <summary>
    /// The ID of the item.
    /// </summary>
    [JsonPropertyName("itemid")]
    [JsonPropertyOrder(0)]
    public int ItemId { get; set; } = 0;

    /// <summary>
    /// The type of mention.
    /// </summary>
    [JsonPropertyName("mentionType")]
    [JsonPropertyOrder(1)]
    public required string MentionType { get; set; }

    /// <summary>
    /// The Microsoft Resource Identifier (MRI) of the user.
    /// </summary>
    [JsonPropertyName("mri")]
    [JsonPropertyOrder(2)]
    public required string Mri { get; set; }

    /// <summary>
    /// The display name of the user.
    /// </summary>
    [JsonPropertyName("displayName")]
    [JsonPropertyOrder(3)]
    public string? DisplayName { get; set; }
}