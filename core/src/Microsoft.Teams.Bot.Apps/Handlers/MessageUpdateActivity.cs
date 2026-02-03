// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Teams.Bot.Apps.Schema;
using Microsoft.Teams.Bot.Core.Schema;

namespace Microsoft.Teams.Bot.Apps.Handlers;

/// <summary>
/// Represents a message update activity.
/// </summary>
public class MessageUpdateActivity : MessageActivity
{
    /// <summary>
    /// Convenience method to create a MessageUpdateActivity from a CoreActivity.
    /// </summary>
    /// <param name="activity">The CoreActivity to convert.</param>
    /// <returns>A MessageUpdateActivity instance.</returns>
    public static new MessageUpdateActivity FromActivity(CoreActivity activity)
    {
        ArgumentNullException.ThrowIfNull(activity);
        return new MessageUpdateActivity(activity);
    }

    /// <summary>
    /// Deserializes a JSON string into a MessageUpdateActivity instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>A MessageUpdateActivity instance.</returns>
    public static new MessageUpdateActivity FromJsonString(string json)
    {
        return FromJsonString(json, TeamsActivityJsonContext.Default.MessageUpdateActivity);
    }

    /// <summary>
    /// Serializes the MessageUpdateActivity to JSON with all message update-specific properties.
    /// </summary>
    /// <returns>JSON string representation of the MessageUpdateActivity</returns>
    public override string ToJson()
        => ToJson(TeamsActivityJsonContext.Default.MessageUpdateActivity);

    /// <summary>
    /// Default constructor.
    /// </summary>
    [JsonConstructor]
    public MessageUpdateActivity() : base()
    {
        Type = TeamsActivityType.MessageUpdate;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageUpdateActivity"/> class with the specified text.
    /// </summary>
    /// <param name="text">The text content of the message.</param>
    public MessageUpdateActivity(string text) : base(text)
    {
        Type = TeamsActivityType.MessageUpdate;
    }

    /// <summary>
    /// Internal constructor to create MessageUpdateActivity from CoreActivity.
    /// </summary>
    /// <param name="activity">The CoreActivity to convert.</param>
    protected MessageUpdateActivity(CoreActivity activity) : base(activity)
    {
        Type = TeamsActivityType.MessageUpdate;
    }
}
