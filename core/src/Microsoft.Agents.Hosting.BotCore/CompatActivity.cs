// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Agents.Core.Models;
using Microsoft.Agents.Core.Serialization;
using Microsoft.Teams.Bot.Core.Schema;

namespace Microsoft.Agents.Hosting.BotCore;

/// <summary>
/// Extension methods for converting between CoreActivity and Microsoft.Agents.Core.Models.Activity.
/// </summary>
public static class CompatActivity
{
    /// <summary>
    /// Converts a CoreActivity to a Microsoft.Agents.Core.Models.Activity.
    /// </summary>
    /// <param name="activity">The CoreActivity to convert.</param>
    /// <returns>A Microsoft.Agents.Core.Models.Activity instance.</returns>
    public static Activity ToAgentsActivity(this CoreActivity activity)
    {
        ArgumentNullException.ThrowIfNull(activity);

        // Serialize CoreActivity to JSON and deserialize to Agents Activity
        var json = activity.ToJson();
        return ProtocolJsonSerializer.ToObject<Activity>(json);
    }

    /// <summary>
    /// Converts a Microsoft.Agents.Core.Models.IActivity to a CoreActivity.
    /// </summary>
    /// <param name="activity">The IActivity to convert.</param>
    /// <returns>A CoreActivity instance.</returns>
    public static CoreActivity ToCoreActivity(this IActivity activity)
    {
        ArgumentNullException.ThrowIfNull(activity);

        // Serialize Agents Activity to JSON and deserialize to CoreActivity
        var json = ProtocolJsonSerializer.ToJson(activity);
        return CoreActivity.FromJsonString(json);
    }
}
