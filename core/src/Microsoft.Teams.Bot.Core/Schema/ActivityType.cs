// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Teams.Bot.Core.Schema;

/// <summary>
/// Provides constant values that represent activity types used in messaging workflows.
/// </summary>
/// <remarks>Use the fields of this class to specify or compare activity types in message-based systems. This
/// class is typically used to avoid hardcoding string literals for activity type identifiers.</remarks>
public static class ActivityType
{
    /// <summary>
    /// Represents the default message string used for communication or display purposes.
    /// </summary>
    public const string Message = "message";
    /// <summary>
    /// Represents a typing indicator activity.
    /// </summary>
    public const string Typing = "typing";
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
}
