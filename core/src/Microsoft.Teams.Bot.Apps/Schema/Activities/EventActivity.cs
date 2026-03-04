// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Teams.Bot.Core.Schema;

namespace Microsoft.Teams.Bot.Apps.Schema;

/// <summary>
/// Represents an event activity.
/// </summary>
public class EventActivity : TeamsActivity
{
    /// <summary>
    /// Creates an EventActivity from a CoreActivity.
    /// </summary>
    public static new EventActivity FromActivity(CoreActivity activity)
    {
        ArgumentNullException.ThrowIfNull(activity);
        return new EventActivity(activity);
    }

    /// <summary>
    /// Gets or sets the name of the event. See <see cref="EventNames"/> for common values.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="EventActivity"/> class.
    /// </summary>
    [JsonConstructor]
    public EventActivity() : base(TeamsActivityType.Event)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EventActivity"/> class with the specified name.
    /// </summary>
    public EventActivity(string name) : base(TeamsActivityType.Event)
    {
        Name = name;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EventActivity"/> class from a CoreActivity.
    /// </summary>
    protected EventActivity(CoreActivity activity) : base(activity)
    {
        if (activity.Properties.TryGetValue("name", out object? name))
        {
            Name = name?.ToString();
            activity.Properties.Remove("name");
        }
    }
}

/// <summary>
/// Represents an event activity with a strongly-typed value.
/// </summary>
/// <typeparam name="TValue">The type of the value payload.</typeparam>
public class EventActivity<TValue> : EventActivity
{
    /// <summary>
    /// Gets or sets the strongly-typed value associated with the event activity.
    /// Shadows the base class Value property, deserializing from the underlying JsonNode on access.
    /// </summary>
    public new TValue? Value
    {
        get => base.Value != null ? JsonSerializer.Deserialize<TValue>(base.Value.ToJsonString()) : default;
        set => base.Value = value != null ? JsonSerializer.SerializeToNode(value) : null;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EventActivity{TValue}"/> class.
    /// </summary>
    public EventActivity() : base()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EventActivity{TValue}"/> class with the specified name.
    /// </summary>
    public EventActivity(string name) : base(name)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EventActivity{TValue}"/> class from an EventActivity.
    /// </summary>
    public EventActivity(EventActivity activity) : base(activity)
    {
    }
}

/// <summary>
/// String constants for event activity names.
/// </summary>
public static class EventNames
{
    /// <summary>Meeting start event name.</summary>
    public const string MeetingStart = "application/vnd.microsoft.meetingStart";

    /// <summary>Meeting end event name.</summary>
    public const string MeetingEnd = "application/vnd.microsoft.meetingEnd";

    /// <summary>Meeting participant join event name.</summary>
    public const string MeetingParticipantJoin = "application/vnd.microsoft.meetingParticipantJoin";

    /// <summary>Meeting participant leave event name.</summary>
    public const string MeetingParticipantLeave = "application/vnd.microsoft.meetingParticipantLeave";

    //TODO : review read receipts
    /*
    /// <summary>Read receipt event name. Fired when a user reads a message in a 1:1 chat with the bot.</summary>
    public const string ReadReceipt = "application/vnd.microsoft.readReceipt";
    */
}
