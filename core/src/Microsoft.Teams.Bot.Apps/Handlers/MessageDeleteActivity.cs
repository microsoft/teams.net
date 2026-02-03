// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Teams.Bot.Apps.Schema;
using Microsoft.Teams.Bot.Core.Schema;

namespace Microsoft.Teams.Bot.Apps.Handlers;

/// <summary>
/// Represents a message delete activity.
/// </summary>
public class MessageDeleteActivity : TeamsActivity
{
    /// <summary>
    /// Convenience method to create a MessageDeleteActivity from a CoreActivity.
    /// </summary>
    /// <param name="activity">The CoreActivity to convert.</param>
    /// <returns>A MessageDeleteActivity instance.</returns>
    public static new MessageDeleteActivity FromActivity(CoreActivity activity)
    {
        ArgumentNullException.ThrowIfNull(activity);
        return new MessageDeleteActivity(activity);
    }

    /// <summary>
    /// Deserializes a JSON string into a MessageDeleteActivity instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>A MessageDeleteActivity instance.</returns>
    public static new MessageDeleteActivity FromJsonString(string json)
    {
        return FromJsonString(json, TeamsActivityJsonContext.Default.MessageDeleteActivity);
    }

    /// <summary>
    /// Serializes the MessageDeleteActivity to JSON with all message delete-specific properties.
    /// </summary>
    /// <returns>JSON string representation of the MessageDeleteActivity</returns>
    public override string ToJson()
        => ToJson(TeamsActivityJsonContext.Default.MessageDeleteActivity);

    /// <summary>
    /// Default constructor.
    /// </summary>
    [JsonConstructor]
    public MessageDeleteActivity() : base(TeamsActivityType.MessageDelete)
    {
    }

    /// <summary>
    /// Internal constructor to create MessageDeleteActivity from CoreActivity.
    /// </summary>
    /// <param name="activity">The CoreActivity to convert.</param>
    protected MessageDeleteActivity(CoreActivity activity) : base(activity)
    {
    }
}
