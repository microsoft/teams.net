// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Api.Messages;

/// <summary>
/// The type of
/// conversation, whether a team or channel. Possible values include: 'team',
/// 'channel'
/// </summary>
[JsonConverter(typeof(JsonConverter<ConversationIdentityType>))]
public class ConversationIdentityType(string value) : StringEnum(value)
{
    public static readonly ConversationIdentityType Team = new("team");
    public bool IsTeam => Team.Equals(Value);

    public static readonly ConversationIdentityType Channel = new("channel");
    public bool IsChannel => Channel.Equals(Value);
}

/// <summary>
/// Represents a team or channel entity.
/// </summary>
public class Conversation
{
    /// <summary>
    /// The type of
    /// conversation, whether a team or channel. Possible values include: 'team',
    /// 'channel'
    /// </summary>
    [JsonPropertyName("conversationIdentityType")]
    [JsonPropertyOrder(0)]
    public ConversationIdentityType? ConversationIdentityType { get; set; }

    /// <summary>
    /// The id of the team or channel.
    /// </summary>
    [JsonPropertyName("id")]
    [JsonPropertyOrder(1)]
    public required string Id { get; set; }

    /// <summary>
    /// The plaintext display name of the team or channel entity.
    /// </summary>
    [JsonPropertyName("displayName")]
    [JsonPropertyOrder(2)]
    public string? DisplayName { get; set; }
}