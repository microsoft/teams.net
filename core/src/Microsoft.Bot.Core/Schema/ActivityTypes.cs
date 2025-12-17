// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Bot.Core.Schema;

/// <summary>
/// Provides constant values that represent activity types used in messaging workflows.
/// </summary>
/// <remarks>Use the fields of this class to specify or compare activity types in message-based systems. This
/// class is typically used to avoid hardcoding string literals for activity type identifiers.</remarks>
public static class ActivityTypes
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
    /// Represents a command activity.
    /// </summary>
    public const string Command = "command";
    
    /// <summary>
    /// Represents a command result activity.
    /// </summary>
    public const string CommandResult = "commandResult";
    
    /// <summary>
    /// Represents a conversation update activity.
    /// </summary>
    public const string ConversationUpdate = "conversationUpdate";
    
    /// <summary>
    /// Represents an end of conversation activity.
    /// </summary>
    public const string EndOfConversation = "endOfConversation";
    
    /// <summary>
    /// Represents an installation update activity.
    /// </summary>
    public const string InstallUpdate = "installationUpdate";
    
    /// <summary>
    /// Represents a message update activity.
    /// </summary>
    public const string MessageUpdate = "messageUpdate";
    
    /// <summary>
    /// Represents a message delete activity.
    /// </summary>
    public const string MessageDelete = "messageDelete";
    
    /// <summary>
    /// Represents a message reaction activity.
    /// </summary>
    public const string MessageReaction = "messageReaction";
    
    /// <summary>
    /// Represents an event activity.
    /// </summary>
    public const string Event = "event";
    
    /// <summary>
    /// Represents an invoke activity.
    /// </summary>
    public const string Invoke = "invoke";
}
