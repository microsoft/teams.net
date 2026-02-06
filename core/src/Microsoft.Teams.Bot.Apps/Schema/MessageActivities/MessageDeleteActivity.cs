// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Teams.Bot.Core.Schema;

namespace Microsoft.Teams.Bot.Apps.Schema.MessageActivities;

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
