// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Core.Activities;

/// <summary>
/// End of conversation code constants.
/// </summary>
public static class EndOfConversationCodes
{
    /// <summary>
    /// Unknown end of conversation code.
    /// </summary>
    public const string Unknown = "unknown";

    /// <summary>
    /// Completed successfully.
    /// </summary>
    public const string CompletedSuccessfully = "completedSuccessfully";

    /// <summary>
    /// User cancelled.
    /// </summary>
    public const string UserCancelled = "userCancelled";

    /// <summary>
    /// Bot timed out.
    /// </summary>
    public const string BotTimedOut = "botTimedOut";

    /// <summary>
    /// Bot issued invalid message.
    /// </summary>
    public const string BotIssuedInvalidMessage = "botIssuedInvalidMessage";

    /// <summary>
    /// Channel failed.
    /// </summary>
    public const string ChannelFailed = "channelFailed";
}

/// <summary>
/// Represents an end of conversation activity.
/// </summary>
public class EndOfConversationActivity : Activity
{
    /// <summary>
    /// Gets or sets the code indicating why the conversation ended.
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    /// Gets or sets the text content of the message.
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="EndOfConversationActivity"/> class.
    /// </summary>
    public EndOfConversationActivity() : base(ActivityTypes.EndOfConversation)
    {
    }
}
