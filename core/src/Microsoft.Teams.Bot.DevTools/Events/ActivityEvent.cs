// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Microsoft.Teams.Bot.Core.Schema;

namespace Microsoft.Teams.Bot.DevTools.Events;

/// <summary>
/// Event emitted over WebSocket when an activity is received, sent, or errors.
/// Wire format must match what the embedded React UI expects.
/// </summary>
public class ActivityEvent : IDevToolsEvent
{
    /// <inheritdoc/>
    [JsonPropertyName("id")]
    [JsonPropertyOrder(0)]
    public Guid Id { get; }

    /// <inheritdoc/>
    [JsonPropertyName("type")]
    [JsonPropertyOrder(1)]
    public string Type { get; }

    /// <inheritdoc/>
    [JsonPropertyName("body")]
    [JsonPropertyOrder(2)]
    public object? Body { get; }

    /// <summary>
    /// Conversation info for the React UI. Serializes with "id", "type", "name" properties.
    /// </summary>
    [JsonPropertyName("chat")]
    [JsonPropertyOrder(3)]
    public object Chat { get; set; }

    /// <summary>
    /// Error details, if this is an error event.
    /// </summary>
    [JsonPropertyName("error")]
    [JsonPropertyOrder(4)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Error { get; set; }

    /// <inheritdoc/>
    [JsonPropertyName("sentAt")]
    [JsonPropertyOrder(5)]
    public DateTime SentAt { get; }

    /// <summary>
    /// Creates a new activity event.
    /// </summary>
    /// <param name="type">The activity event type suffix (received, sent, error).</param>
    /// <param name="activity">The activity this event relates to.</param>
    public ActivityEvent(string type, CoreActivity activity)
    {
        ArgumentNullException.ThrowIfNull(activity);

        Id = Guid.NewGuid();
        Type = $"activity.{type}";
        Body = activity;
        SentAt = DateTime.Now;

        // Build chat object matching what the React UI expects.
        // Core's Conversation only has Id + extension data, so we read type/name
        // from extension properties if available, otherwise use defaults.
        var conversation = activity.Conversation;
        Chat = new ChatInfo
        {
            Id = conversation?.Id ?? "unknown",
            Type = conversation?.Properties.TryGetValue("type", out var t) == true
                ? t?.ToString() ?? "personal"
                : "personal",
            Name = conversation?.Properties.TryGetValue("name", out var n) == true
                ? n?.ToString() ?? "default"
                : "default"
        };
    }

    /// <summary>
    /// Creates a "received" activity event.
    /// </summary>
    public static ActivityEvent Received(CoreActivity activity) => new("received", activity);

    /// <summary>
    /// Creates a "sent" activity event.
    /// </summary>
    public static ActivityEvent Sent(CoreActivity activity) => new("sent", activity);

    /// <summary>
    /// Creates an "error" activity event.
    /// </summary>
    public static ActivityEvent Err(CoreActivity activity, object error)
        => new("error", activity) { Error = error };
}

/// <summary>
/// Serializable chat info matching the wire format expected by the React UI.
/// </summary>
internal sealed class ChatInfo
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "unknown";

    [JsonPropertyName("type")]
    public string Type { get; set; } = "personal";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "default";
}
