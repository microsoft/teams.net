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
    /// 👀
    /// </summary>
    public static readonly ReactionType Eyes = new("1f440_eyes");
    public bool IsEyes => Eyes.Equals(Value);

    /// <summary>
    /// ✅
    /// </summary>
    public static readonly ReactionType CheckMark = new("2705_whiteheavycheckmark");
    public bool IsCheckMark => CheckMark.Equals(Value);

    /// <summary>
    /// 🚀
    /// </summary>
    public static readonly ReactionType Launch = new("launch");
    public bool IsLaunch => Launch.Equals(Value);

    /// <summary>
    /// 📌
    /// </summary>
    public static readonly ReactionType Pushpin = new("1f4cc_pushpin");
    public bool IsPushpin => Pushpin.Equals(Value);
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