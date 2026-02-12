// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Microsoft.Teams.Bot.Core.Schema;

namespace Microsoft.Teams.Bot.Apps.Schema.MessageActivities;

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
