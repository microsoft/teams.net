using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Api.Messages;

/// <summary>
/// The type of reaction given to the
/// message. Possible values include: 'like', 'heart', 'laugh', 'surprised',
/// 'sad', 'angry', 'plusOne'
/// </summary>
[JsonConverter(typeof(JsonConverter<ReactionType>))]
public class ReactionType(string value) : StringEnum(value)
{
    public static readonly ReactionType Like = new("like");
    public bool IsLike => Like.Equals(Value);

    public static readonly ReactionType Heart = new("heart");
    public bool IsHeart => Heart.Equals(Value);

    public static readonly ReactionType Laugh = new("laugh");
    public bool IsLaugh => Laugh.Equals(Value);

    public static readonly ReactionType Surprise = new("surprise");
    public bool IsSurprise => Surprise.Equals(Value);

    public static readonly ReactionType Sad = new("sad");
    public bool IsSad => Sad.Equals(Value);

    public static readonly ReactionType Angry = new("angry");
    public bool IsAngry => Angry.Equals(Value);

    public static readonly ReactionType PlusOne = new("plusOne");
    public bool IsPlusOne => PlusOne.Equals(Value);
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