// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Core.Activities;

/// <summary>
/// Represents an end of conversation activity.
/// </summary>
public class EndOfConversationActivity : Activity
{
    /// <summary>
    /// Gets or sets the code for endOfConversation activities that indicates why the conversation ended.
    /// See <see cref="EndOfConversationCodes"/> for common values.
    /// </summary>
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    /// <summary>
    /// Gets or sets the text content of the message.
    /// </summary>
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="EndOfConversationActivity"/> class.
    /// </summary>
    public EndOfConversationActivity() : base(ActivityTypes.EndOfConversation)
    {
    }
}

/// <summary>
/// String constants for end of conversation codes.
/// </summary>
public static class EndOfConversationCodes
{
    /// <summary>
    /// Unknown reason for ending the conversation.
    /// </summary>
    public const string Unknown = "unknown";

    /// <summary>
    /// Conversation completed successfully.
    /// </summary>
    public const string CompletedSuccessfully = "completedSuccessfully";

    /// <summary>
    /// User cancelled the conversation.
    /// </summary>
    public const string UserCancelled = "userCancelled";

    /// <summary>
    /// Bot timed out.
    /// </summary>
    public const string BotTimedOut = "botTimedOut";

    /// <summary>
    /// Bot issued an invalid message.
    /// </summary>
    public const string BotIssuedInvalidMessage = "botIssuedInvalidMessage";

    /// <summary>
    /// Channel failed.
    /// </summary>
    public const string ChannelFailed = "channelFailed";
}
