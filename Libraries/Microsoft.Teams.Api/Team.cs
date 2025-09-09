﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Teams.Api;

/// <summary>
/// Team
/// </summary>
public class Team
{
    /// <summary>
    /// Unique identifier representing a team
    /// </summary>
    [JsonPropertyName("id")]
    [JsonPropertyOrder(0)]
    public required string Id { get; set; }

    /// <summary>
    /// The Azure AD Team group Id
    /// </summary>
    [JsonPropertyName("aadGroupId")]
    [JsonPropertyOrder(1)]
    public string? AadGroupId { get; set; }

    /// <summary>
    /// The tenant Id of the Team
    /// </summary>
    [JsonPropertyName("tenantId")]
    [JsonPropertyOrder(2)]
    public string? TenantId { get; set; }

    /// <summary>
    /// The type of the team
    /// </summary>
    [JsonPropertyName("type")]
    [JsonPropertyOrder(3)]
    public TeamType? Type { get; set; }

    /// <summary>
    /// The team name
    /// </summary>
    [JsonPropertyName("name")]
    [JsonPropertyOrder(4)]
    public string? Name { get; set; }

    /// <summary>
    /// Count of channels in the team
    /// </summary>
    [JsonPropertyName("channelCount")]
    [JsonPropertyOrder(5)]
    public int? ChannelCount { get; set; }

    /// <summary>
    /// Count of the members in the team
    /// </summary>
    [JsonPropertyName("memberCount")]
    [JsonPropertyOrder(6)]
    public int? MemberCount { get; set; }
}

[JsonConverter(typeof(JsonConverter<TeamType>))]
public class TeamType(string value) : Common.StringEnum(value)
{
    public static readonly TeamType Standard = new("standard");
    public bool IsStandard => Standard.Equals(Value);

    public static readonly TeamType SharedChannel = new("sharedChannel");
    public bool IsSharedChannel => SharedChannel.Equals(Value);

    public static readonly TeamType PrivateChannel = new("privateChannel");
    public bool IsPrivateChannel => PrivateChannel.Equals(Value);
}