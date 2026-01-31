// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Teams.Bot.Core.Schema;

namespace Microsoft.Teams.Bot.Apps.Schema.ConversationActivities;

/// <summary>
/// Represents an end of conversation activity.
/// </summary>
public class EndOfConversationActivity : TeamsActivity
{
    /// <summary>
    /// Convenience method to create an EndOfConversationActivity from a CoreActivity.
    /// </summary>
    /// <param name="activity">The CoreActivity to convert.</param>
    /// <returns>An EndOfConversationActivity instance.</returns>
    public static new EndOfConversationActivity FromActivity(CoreActivity activity)
    {
        ArgumentNullException.ThrowIfNull(activity);
        return new EndOfConversationActivity(activity);
    }

    /// <summary>
    /// Deserializes a JSON string into an EndOfConversationActivity instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>An EndOfConversationActivity instance.</returns>
    public static new EndOfConversationActivity FromJsonString(string json)
    {
        return FromJsonString(json, TeamsActivityJsonContext.Default.EndOfConversationActivity);
    }

    /// <summary>
    /// Serializes the EndOfConversationActivity to JSON.
    /// </summary>
    /// <returns>JSON string representation of the EndOfConversationActivity</returns>
    public override string ToJson()
        => ToJson(TeamsActivityJsonContext.Default.EndOfConversationActivity);

    /// <summary>
    /// Default constructor.
    /// </summary>
    [JsonConstructor]
    public EndOfConversationActivity() : base(TeamsActivityType.EndOfConversation)
    {
    }

    /// <summary>
    /// Internal constructor to create EndOfConversationActivity from CoreActivity.
    /// </summary>
    /// <param name="activity">The CoreActivity to convert.</param>
    protected EndOfConversationActivity(CoreActivity activity) : base(activity)
    {
        if (activity.Properties.TryGetValue("code", out var code))
            Code = code?.ToString();
        if (activity.Properties.TryGetValue("text", out var text))
            Text = text?.ToString();
    }

    /// <summary>
    /// Gets or sets the code indicating why the conversation ended. See <see cref="EndOfConversationCodes"/> for known values.
    /// </summary>
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    /// <summary>
    /// Gets or sets the text content of the message.
    /// </summary>
    [JsonPropertyName("text")]
    public string? Text { get; set; }
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
