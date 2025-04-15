using System.Text.Json.Serialization;

namespace Microsoft.Teams.Api.Messages;

/// <summary>
/// Represents a user, application, or conversation type that either sent or was
/// referenced in a message.
/// </summary>
public class From
{
    /// <summary>
    /// Represents details of the user.
    /// </summary>
    [JsonPropertyName("user")]
    [JsonPropertyOrder(0)]
    public User? User { get; set; }

    /// <summary>
    /// Represents details of the app.
    /// </summary>
    [JsonPropertyName("application")]
    [JsonPropertyOrder(1)]
    public App? Application { get; set; }

    /// <summary>
    /// Represents details of the converesation.
    /// </summary>
    [JsonPropertyName("conversation")]
    [JsonPropertyOrder(2)]
    public Conversation? Conversation { get; set; }
}