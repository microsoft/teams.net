// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Bot.Core.Schema;

namespace Microsoft.Teams.BotApps.Schema;

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
    /// Represents an invoke activity.
    /// </summary>
    public const string Invoke = ActivityType.Invoke;

    /// <summary>
    /// Conversation update activity type.
    /// </summary>
    public static readonly string ConversationUpdate = "conversationUpdate";
    /// <summary>
    /// Installation update activity type.
    /// </summary>
    public static readonly string InstallationUpdate = "installationUpdate";
    /// <summary>
    /// Message reaction activity type.
    /// </summary>
    public static readonly string MessageReaction = "messageReaction";
    
}
