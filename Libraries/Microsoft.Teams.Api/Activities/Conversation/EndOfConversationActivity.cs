// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Api.Activities;

public partial class ActivityType : StringEnum
{
    public static readonly ActivityType EndOfConversation = new("endOfConversation");
    public bool IsEndOfConversation => EndOfConversation.Equals(Value);
}

public class EndOfConversationActivity() : Activity(ActivityType.EndOfConversation)
{
    /// <summary>
    /// The a code for endOfConversation activities that indicates why the conversation ended.
    /// </summary>
    [JsonPropertyName("code")]
    [JsonPropertyOrder(31)]
    public EndOfConversationCode? Code { get; set; }

    /// <summary>
    /// The text content of the message.
    /// </summary>
    [JsonPropertyName("text")]
    [JsonPropertyOrder(32)]
    public required string Text { get; set; }
}

[JsonConverter(typeof(JsonConverter<EndOfConversationCode>))]
public class EndOfConversationCode(string value) : StringEnum(value)
{
    public static readonly EndOfConversationCode Unknown = new("unknown");
    public bool IsUnknown => Unknown.Equals(Value);

    public static readonly EndOfConversationCode CompletedSuccessfully = new("completedSuccessfully");
    public bool IsCompletedSuccessfully => CompletedSuccessfully.Equals(Value);

    public static readonly EndOfConversationCode UserCancelled = new("userCancelled");
    public bool IsUserCancelled => UserCancelled.Equals(Value);

    public static readonly EndOfConversationCode BotTimedOut = new("botTimedOut");
    public bool IsBotTimedOut => BotTimedOut.Equals(Value);

    public static readonly EndOfConversationCode BotIssuedInvalidMessage = new("botIssuedInvalidMessage");
    public bool IsBotIssuedInvalidMessage => BotIssuedInvalidMessage.Equals(Value);

    public static readonly EndOfConversationCode ChannelFailed = new("channelFailed");
    public bool IsChannelFailed => ChannelFailed.Equals(Value);
}