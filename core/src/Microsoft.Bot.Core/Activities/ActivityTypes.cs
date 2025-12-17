// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Core.Activities;

/// <summary>
/// String constants for activity types.
/// </summary>
public static class ActivityTypes
{
    /// <summary>
    /// Message activity type.
    /// </summary>
    public const string Message = "message";

    /// <summary>
    /// Typing activity type.
    /// </summary>
    public const string Typing = "typing";

    /// <summary>
    /// Event activity type.
    /// </summary>
    public const string Event = "event";

    /// <summary>
    /// Command activity type.
    /// </summary>
    public const string Command = "command";

    /// <summary>
    /// Command result activity type.
    /// </summary>
    public const string CommandResult = "commandResult";

    /// <summary>
    /// Installation update activity type.
    /// </summary>
    public const string InstallationUpdate = "installationUpdate";

    /// <summary>
    /// Conversation update activity type.
    /// </summary>
    public const string ConversationUpdate = "conversationUpdate";

    /// <summary>
    /// End of conversation activity type.
    /// </summary>
    public const string EndOfConversation = "endOfConversation";

    /// <summary>
    /// Invoke activity type.
    /// </summary>
    public const string Invoke = "invoke";
}
