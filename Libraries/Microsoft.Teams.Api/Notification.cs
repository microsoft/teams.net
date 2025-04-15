using System.Text.Json.Serialization;

namespace Microsoft.Teams.Api;

/// <summary>
/// Specifies if a notification is to be sent for the mentions.
/// </summary>
public class Notification
{
    /// <summary>
    /// true if notification is to be sent to the user, false otherwise
    /// </summary>
    [JsonPropertyName("alert")]
    [JsonPropertyOrder(0)]
    public bool? Alert { get; set; }

    /// <summary>
    /// true if a notification is to be shown to the user while in a meeting, false otherwise
    /// </summary>
    [JsonPropertyName("alertInMeeting")]
    [JsonPropertyOrder(1)]
    public bool? AlertInMeeting { get; set; }

    /// <summary>
    /// the value of the notification's external resource url
    /// </summary>
    [JsonPropertyName("externalResourceUrl")]
    [JsonPropertyOrder(2)]
    public string? ExternalResourceUrl { get; set; }
}