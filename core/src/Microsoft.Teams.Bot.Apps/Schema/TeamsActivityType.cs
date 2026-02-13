// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Bot.Apps.Schema.ConversationActivities;
using Microsoft.Teams.Bot.Apps.Schema.InstallActivities;
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
    public const string MessageReaction = "messageReaction";
    /// <summary>
    /// Represents a message update activity.
    /// </summary>
    public const string MessageUpdate = "messageUpdate";
    /// <summary>
    /// Represents a message delete activity.
    /// </summary>
    public const string MessageDelete = "messageDelete";

    /// <summary>
    /// Represents a conversation update activity.
    /// </summary>
    public const string ConversationUpdate = "conversationUpdate";

    /*
    /// <summary>
    /// Represents an end of conversation activity.
    /// </summary>
    public const string EndOfConversation = "endOfConversation";
    */

    /// <summary>
    /// Represents an installation update activity.
    /// </summary>
    public const string InstallationUpdate = "installationUpdate";

    /// <summary>
    /// Represents the string value "invoke" used to identify an invoke operation or action.
    /// </summary>
    public const string Invoke = "invoke";

    /// <summary>
    /// Registry of activity type factories for creating specialized activity instances.
    /// </summary>
    internal static readonly Dictionary<string, Func<CoreActivity, TeamsActivity>> ActivityDeserializerMap = new()
    {
        [TeamsActivityType.Message] = MessageActivity.FromActivity,
        [TeamsActivityType.MessageReaction] = MessageReactionActivity.FromActivity,
        [TeamsActivityType.MessageUpdate] = MessageUpdateActivity.FromActivity,
        [TeamsActivityType.MessageDelete] = MessageDeleteActivity.FromActivity,
        [TeamsActivityType.ConversationUpdate] = ConversationUpdateActivity.FromActivity,
        //[TeamsActivityType.EndOfConversation] = EndOfConversationActivity.FromActivity,
        [TeamsActivityType.InstallationUpdate] = InstallUpdateActivity.FromActivity,
        [TeamsActivityType.Invoke] = InvokeActivity.FromActivity,
    };

    /// <summary>
    /// Registry of serialization functions for specialized activity instances.
    /// Maps activity types to functions that serialize the activity using the appropriate JsonTypeInfo.
    /// </summary>
    internal static readonly Dictionary<string, Func<TeamsActivity, string>> ActivitySerializerMap = new()
    {
        [TeamsActivityType.Message] = activity => activity.ToJson(TeamsActivityJsonContext.Default.MessageActivity),
        [TeamsActivityType.Invoke] = activity => activity.ToJson(TeamsActivityJsonContext.Default.InvokeActivity),
    };
}
