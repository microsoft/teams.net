// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Teams.Bot.Core.Schema;

namespace Microsoft.Teams.Bot.Apps.Schema.MessageActivities;

/// <summary>
/// Represents a message reaction activity.
/// </summary>
public class MessageReactionActivity : TeamsActivity
{
    /// <summary>
    /// Convenience method to create a MessageReactionActivity from a CoreActivity.
    /// </summary>
    /// <param name="activity">The CoreActivity to convert.</param>
    /// <returns>A MessageReactionActivity instance.</returns>
    public static new MessageReactionActivity FromActivity(CoreActivity activity)
    {
        ArgumentNullException.ThrowIfNull(activity);
        return new MessageReactionActivity(activity);
    }

    /// <summary>
    /// Default constructor.
    /// </summary>
    [JsonConstructor]
    public MessageReactionActivity() : base(TeamsActivityType.MessageReaction)
    {
    }

    /// <summary>
    /// Internal constructor to create MessageReactionActivity from CoreActivity.
    /// </summary>
    /// <param name="activity">The CoreActivity to convert.</param>
    protected MessageReactionActivity(CoreActivity activity) : base(activity)
    {
        if (activity.Properties.TryGetValue("reactionsAdded", out var reactionsAdded) && reactionsAdded != null)
        {
            if (reactionsAdded is JsonElement je)
            {
                ReactionsAdded = JsonSerializer.Deserialize<List<MessageReaction>>(je.GetRawText());
            }
            else
            {
                ReactionsAdded = reactionsAdded as IList<MessageReaction>;
            }
            activity.Properties.Remove("reactionsAdded");
        }
        if (activity.Properties.TryGetValue("reactionsRemoved", out var reactionsRemoved) && reactionsRemoved != null)
        {
            if (reactionsRemoved is JsonElement je)
            {
                ReactionsRemoved = JsonSerializer.Deserialize<List<MessageReaction>>(je.GetRawText());
            }
            else
            {
                ReactionsRemoved = reactionsRemoved as IList<MessageReaction>;
            }
            activity.Properties.Remove("reactionsRemoved");
        }
        if (activity.Properties.TryGetValue("replyToId", out var replyToId) && replyToId != null)
        {
            if (replyToId is JsonElement jeReplyToId && jeReplyToId.ValueKind == JsonValueKind.String)
            {
                ReplyToId = jeReplyToId.GetString();
            }
            else
            {
                ReplyToId = replyToId.ToString();
            }
            activity.Properties.Remove("replyToId");
        }
    }

    /// <summary>
    /// Gets or sets the reactions added to the message.
    /// </summary>
    [JsonPropertyName("reactionsAdded")]
    public IList<MessageReaction>? ReactionsAdded { get; set; }

    /// <summary>
    /// Gets or sets the reactions removed from the message.
    /// </summary>
    [JsonPropertyName("reactionsRemoved")]
    public IList<MessageReaction>? ReactionsRemoved { get; set; }

    /// <summary>
    /// Gets or sets the ID of the message being reacted to.
    /// </summary>
    [JsonPropertyName("replyToId")]
    public string? ReplyToId { get; set; }
}

/// <summary>
/// Represents a reaction to a message.
/// </summary>
public class MessageReaction
{
    /// <summary>
    /// Gets or sets the type of reaction.
    /// See <see cref="ReactionTypes"/> for common values.
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }
}

/// <summary>
/// String constants for reaction types.
/// </summary>
public static class ReactionTypes
{
    /// <summary>
    /// Like reaction.
    /// </summary>
    public const string Like = "like";

    /// <summary>
    /// Heart reaction.
    /// </summary>
    public const string Heart = "heart";

    /// <summary>
    /// Laugh reaction.
    /// </summary>
    public const string Laugh = "laugh";

    /// <summary>
    /// Surprise reaction.
    /// </summary>
    public const string Surprise = "surprise";

    /// <summary>
    /// Sad reaction.
    /// </summary>
    public const string Sad = "sad";

    /// <summary>
    /// Angry reaction.
    /// </summary>
    public const string Angry = "angry";

    /// <summary>
    /// Plus one reaction.
    /// </summary>
    public const string PlusOne = "plusOne";
}
