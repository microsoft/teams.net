// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

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
    /// Represents an event activity.
    /// </summary>
    public const string Event = "event";

    //TODO : review command activity
    /*
    /// <summary>
    /// Represents a command activity.
    /// </summary>
    public const string Command = "command";

    /// <summary>
    /// Represents a command result activity.
    /// </summary>
    public const string CommandResult = "commandResult";
    */

    /// <summary>
    /// Registry of activity type factories for creating specialized activity instances.
    /// </summary>
    internal static readonly Dictionary<string, Func<CoreActivity, TeamsActivity>> ActivityDeserializerMap = new()
    {
        [Message] = MessageActivity.FromActivity,
        [MessageReaction] = MessageReactionActivity.FromActivity,
        [MessageUpdate] = MessageUpdateActivity.FromActivity,
        [MessageDelete] = MessageDeleteActivity.FromActivity,
        [ConversationUpdate] = ConversationUpdateActivity.FromActivity,
        //[TeamsActivityType.EndOfConversation] = EndOfConversationActivity.FromActivity,
        [InstallationUpdate] = InstallUpdateActivity.FromActivity,
        [Invoke] = InvokeActivity.FromActivity,
        [Event] = EventActivity.FromActivity,
        [Command] = CommandActivity.FromActivity,
        [CommandResult] = CommandResultActivity.FromActivity,
    };
}
