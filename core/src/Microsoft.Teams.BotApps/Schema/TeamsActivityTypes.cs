// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

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
    /// Message activity type.
    /// </summary>
    public static readonly string Message = "message";
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
    /// <summary>
    /// Represents a typing indicator activity.
    /// </summary>
    public const string Typing = "typing";
}
