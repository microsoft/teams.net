// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Api.Messages;

/// <summary>
/// The type of reaction given to the message.
/// </summary>
[JsonConverter(typeof(JsonConverter<ReactionType>))]
public class ReactionType(string value) : StringEnum(value)
{
    /// <summary>
    /// 👍
    /// </summary>
    public static readonly ReactionType Like = new("like");
    public bool IsLike => Like.Equals(Value);

    /// <summary>
    /// ❤️
    /// </summary>
    public static readonly ReactionType Heart = new("heart");
    public bool IsHeart => Heart.Equals(Value);

    /// <summary>
    /// ✅
    /// </summary>
    public static readonly ReactionType Checkmark = new("checkmark");
    public bool IsCheckmark => Checkmark.Equals(Value);

    /// <summary>
    /// ⏳
    /// </summary>
    public static readonly ReactionType Hourglass = new("hourglass");
    public bool IsHourglass => Hourglass.Equals(Value);

    /// <summary>
    /// 📌
    /// </summary>
    public static readonly ReactionType Pushpin = new("pushpin");
    public bool IsPushpin => Pushpin.Equals(Value);

    /// <summary>
    /// ❗
    /// </summary>
    public static readonly ReactionType Exclamation = new("exclamation");
    public bool IsExclamation => Exclamation.Equals(Value);

    /// <summary>
    /// 😆
    /// </summary>
    public static readonly ReactionType Laugh = new("laugh");
    public bool IsLaugh => Laugh.Equals(Value);

    /// <summary>
    /// 😮
    /// </summary>
    public static readonly ReactionType Surprise = new("surprise");
    public bool IsSurprise => Surprise.Equals(Value);

    /// <summary>
    /// 🙁
    /// </summary>
    public static readonly ReactionType Sad = new("sad");
    public bool IsSad => Sad.Equals(Value);

    /// <summary>
    /// 😠
    /// </summary>
    public static readonly ReactionType Angry = new("angry");
    public bool IsAngry => Angry.Equals(Value);

}

/// <summary>
/// Message Reaction
/// </summary>
public class Reaction
{
    /// <summary>
    /// The type of reaction given to the
    /// message. Possible values include: 'like', 'heart', 'laugh', 'surprised',
    /// 'sad', 'angry', 'plusOne'
    /// </summary>
    [JsonPropertyName("type")]
    [JsonPropertyOrder(0)]
    public required ReactionType Type { get; set; }

    /// <summary>
    /// Timestamp of when the user reacted to the message.
    /// </summary>
    [JsonPropertyName("createdDateTime")]
    [JsonPropertyOrder(1)]
    public string? CreatedDateTime { get; set; }

    /// <summary>
    /// The user with which the reaction is associated.
    /// </summary>
    [JsonPropertyName("user")]
    [JsonPropertyOrder(2)]
    public User? User { get; set; }
}