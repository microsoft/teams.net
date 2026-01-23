// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Bot.Apps.Schema.MessageActivities;
using Microsoft.Teams.Bot.Core.Schema;

namespace Microsoft.Teams.Bot.Apps.Schema;

/// <summary>
/// Provides constant values for activity types used in Microsoft Teams bot interactions.
/// </summary>
/// <remarks>These activity type constants are used to identify the type of activity received or sent in a Teams
/// bot context. Use these values when handling or generating activities to ensure compatibility with the Teams
/// platform.</remarks>
public static class TeamsActivityType
{

    /// <summary>
    /// Represents the default message string used for communication or display purposes.
    /// </summary>
    public const string Message = ActivityType.Message;
    /// <summary>
    /// Represents a typing indicator activity.
    /// </summary>
    public const string Typing = ActivityType.Typing;

    /// <summary>
    /// Represents a message reaction activity.
    /// </summary>
    public const string MessageReaction = ActivityType.MessageReaction;

    /// <summary>
    /// Represents a message update activity.
    /// </summary>
    public const string MessageUpdate = ActivityType.MessageUpdate;

    /// <summary>
    /// Represents a message delete activity.
    /// </summary>
    public const string MessageDelete = ActivityType.MessageDelete;

    /// <summary>
    /// Registry of activity type factories for creating specialized activity instances.
    /// </summary>
    internal static readonly Dictionary<string, (Func<CoreActivity, TeamsActivity> FromActivity, Func<string, TeamsActivity> FromJson)> ActivityDeserializerMap = new()
    {
        [TeamsActivityType.Message] = (MessageActivity.FromActivity, MessageActivity.FromJsonString),
        [TeamsActivityType.MessageReaction] = (MessageReactionActivity.FromActivity, MessageReactionActivity.FromJsonString),
        [TeamsActivityType.MessageUpdate] = (MessageUpdateActivity.FromActivity, MessageUpdateActivity.FromJsonString),
        [TeamsActivityType.MessageDelete] = (MessageDeleteActivity.FromActivity, MessageDeleteActivity.FromJsonString),
    };
}
