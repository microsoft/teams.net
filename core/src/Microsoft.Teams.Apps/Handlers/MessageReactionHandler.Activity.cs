// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Apps.Utils;
using Microsoft.Teams.Core.Schema;

namespace Microsoft.Teams.Apps;

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
    internal MessageReactionActivity() : base(TeamsActivityTypes.MessageReaction)
    {
    }

    /// <summary>
    /// Internal constructor to create MessageReactionActivity from CoreActivity.
    /// </summary>
    /// <param name="activity">The CoreActivity to convert.</param>
    internal MessageReactionActivity(CoreActivity activity) : base(activity)
    {
        ArgumentNullException.ThrowIfNull(activity);
        ReactionsAdded = Properties.Extract<IList<MessageReaction>>("reactionsAdded");
        ReactionsRemoved = Properties.Extract<IList<MessageReaction>>("reactionsRemoved");
        ReplyToId = Properties.Extract<string>("replyToId");
    }

    /// <summary>
    /// Gets or sets the reactions added to the message.
    /// </summary>
    [JsonPropertyName("reactionsAdded")]
    public IList<MessageReaction>? ReactionsAdded { get; internal set; }

    /// <summary>
    /// Gets or sets the reactions removed from the message.
    /// </summary>
    [JsonPropertyName("reactionsRemoved")]
    public IList<MessageReaction>? ReactionsRemoved { get; internal set; }
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
    public ReactionType? Type { get; internal set; }
}

/// <summary>
/// String constants for reaction types.
/// </summary>
[JsonConverter(typeof(StringEnumJsonConverter<ReactionType>))]
public class ReactionType(string value) : StringEnum(value)
{
    /// <summary>
    /// Like reaction (👍).
    /// </summary>
    public static readonly ReactionType Like = new("like");

    /// <summary>
    /// Heart reaction (❤️).
    /// </summary>
    public static readonly ReactionType Heart = new("heart");

    /// <summary>
    /// Checkmark reaction (✅).
    /// </summary>
    public static readonly ReactionType Checkmark = new("checkmark");

    /// <summary>
    /// Hourglass reaction (⏳).
    /// </summary>
    public static readonly ReactionType Hourglass = new("hourglass");

    /// <summary>
    /// Pushpin reaction (📌).
    /// </summary>
    public static readonly ReactionType Pushpin = new("pushpin");

    /// <summary>
    /// Exclamation reaction (❗).
    /// </summary>
    public static readonly ReactionType Exclamation = new("exclamation");

    /// <summary>
    /// Laugh reaction (😆).
    /// </summary>
    public static readonly ReactionType Laugh = new("laugh");

    /// <summary>
    /// Surprise reaction (😮).
    /// </summary>
    public static readonly ReactionType Surprise = new("surprise");

    /// <summary>
    /// Sad reaction (🙁).
    /// </summary>
    public static readonly ReactionType Sad = new("sad");

    /// <summary>
    /// Angry reaction (😠).
    /// </summary>
    public static readonly ReactionType Angry = new("angry");
}

/// <summary>
/// Common reaction type values.
/// </summary>
public static class ReactionTypes
{
    /// <summary>Like reaction.</summary>
    public static ReactionType Like => ReactionType.Like;
    /// <summary>Heart reaction.</summary>
    public static ReactionType Heart => ReactionType.Heart;
    /// <summary>Checkmark reaction.</summary>
    public static ReactionType Checkmark => ReactionType.Checkmark;
    /// <summary>Hourglass reaction.</summary>
    public static ReactionType Hourglass => ReactionType.Hourglass;
    /// <summary>Pushpin reaction.</summary>
    public static ReactionType Pushpin => ReactionType.Pushpin;
    /// <summary>Exclamation reaction.</summary>
    public static ReactionType Exclamation => ReactionType.Exclamation;
    /// <summary>Laugh reaction.</summary>
    public static ReactionType Laugh => ReactionType.Laugh;
    /// <summary>Surprise reaction.</summary>
    public static ReactionType Surprise => ReactionType.Surprise;
    /// <summary>Sad reaction.</summary>
    public static ReactionType Sad => ReactionType.Sad;
    /// <summary>Angry reaction.</summary>
    public static ReactionType Angry => ReactionType.Angry;
}
