// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Core.Schema;

namespace Microsoft.Teams.Apps.Handlers;

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
    public string? Name { get; internal set; }

    /// <summary>
    /// Gets or sets the value payload type for event activities that carry typed variants.
    /// </summary>
    [JsonPropertyName("valueType")]
    public string? ValueType { get; set; }

    /// <summary>
    /// Gets or sets the value payload of the event activity.
    /// </summary>
    [JsonPropertyName("value")]
    public JsonNode? Value { get; internal set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="EventActivity"/> class.
    /// </summary>
    [JsonConstructor]
    internal EventActivity() : base(TeamsActivityTypes.Event)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EventActivity"/> class from a CoreActivity.
    /// </summary>
    internal EventActivity(CoreActivity activity) : base(activity)
    {
        if (activity is EventActivity evt)
        {
            Name = evt.Name;
            ValueType = evt.ValueType;
            Value = evt.Value;
            return;
        }

        Name = Properties.Extract<string>("name");
        ValueType = Properties.Extract<string>("valueType");
        Value = Properties.Extract<JsonNode>("value");
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
    internal EventActivity() : base()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EventActivity{TValue}"/> class from an EventActivity.
    /// </summary>
    internal EventActivity(EventActivity activity) : base(activity)
    {
    }
}

/// <summary>
/// String constants for event activity names.
/// </summary>
public static class EventNames
{
    /// <summary>Agent 365 lifecycle event name.</summary>
    public const string AgentLifecycle = "agentLifecycle";

    /// <summary>Meeting start event name.</summary>
    public const string MeetingStart = "application/vnd.microsoft.meetingStart";

    /// <summary>Meeting end event name.</summary>
    public const string MeetingEnd = "application/vnd.microsoft.meetingEnd";

    /// <summary>Meeting participant join event name.</summary>
    public const string MeetingParticipantJoin = "application/vnd.microsoft.meetingParticipantJoin";

    /// <summary>Meeting participant leave event name.</summary>
    public const string MeetingParticipantLeave = "application/vnd.microsoft.meetingParticipantLeave";
}
