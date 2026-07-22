// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Core.Schema;

namespace Microsoft.Teams.Apps;

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
#pragma warning disable CS0618 // Inbound subclass legitimately uses the base parameterless ctor.
    internal MessageUpdateActivity() : base()
#pragma warning restore CS0618
    {
        Type = TeamsActivityTypes.MessageUpdate;
    }

    /// <summary>
    /// Internal constructor to create MessageUpdateActivity from CoreActivity.
    /// </summary>
    /// <param name="activity">The CoreActivity to convert.</param>
    internal MessageUpdateActivity(CoreActivity activity) : base(activity)
    {
        Type = TeamsActivityTypes.MessageUpdate;
    }
}
