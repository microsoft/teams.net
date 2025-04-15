using System.Text.Json.Serialization;

namespace Microsoft.Teams.Cards;

/// <summary>
/// Defines how a card can be refreshed by making a request to the target Bot.
/// </summary>
public class Refresh
{
    /// <summary>
    /// The action to be executed to refresh the card. Clients can run this refresh action automatically or can provide an affordance for users to trigger it manually.
    /// </summary>
    [JsonPropertyName("action")]
    [JsonPropertyOrder(0)]
    public object? Action { get; set; }

    /// <summary>
    /// A timestamp that informs a Host when the card content has expired, and that it should trigger a refresh as appropriate. The format is ISO-8601 Instant format. E.g., 2022-01-01T12:00:00Z
    /// </summary>
    [JsonPropertyName("expires")]
    [JsonPropertyOrder(1)]
    public string? Expires { get; set; }

    /// <summary>
    /// A list of user Ids informing the client for which users should the refresh action should be run automatically. Some clients will not run the refresh action automatically unless this property is specified. Some clients may ignore this property and always run the refresh action automatically.
    /// </summary>
    [JsonPropertyName("userIds")]
    [JsonPropertyOrder(2)]
    public IList<string>? UserIds { get; set; }
}