// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

using Microsoft.Teams.Api.Memberships;
using Microsoft.Teams.Common;

namespace Microsoft.Teams.Api;

public class Account
{
    [JsonPropertyName("id")]
    [JsonPropertyOrder(0)]
    public required string Id { get; set; }

    [JsonPropertyName("aadObjectId")]
    [JsonPropertyOrder(1)]
    public string? AadObjectId { get; set; }

    [JsonPropertyName("role")]
    [JsonPropertyOrder(2)]
    public Role? Role { get; set; }

    [JsonPropertyName("name")]
    [JsonPropertyOrder(3)]
    public string? Name { get; set; }

    [JsonPropertyName("membershipSources")]
    [JsonPropertyOrder(4)]
    public IList<MembershipSource>? MembershipSources { get; set; }

    [JsonPropertyName("agenticUserId")]
    [JsonPropertyOrder(5)]
    public string? AgenticUserId { get; set; }

    [JsonPropertyName("agenticAppId")]
    [JsonPropertyOrder(6)]
    public string? AgenticAppId { get; set; }

    [JsonPropertyName("properties")]
    [JsonPropertyOrder(7)]
    public Dictionary<string, object>? Properties { get; set; }
}

[JsonConverter(typeof(JsonConverter<Role>))]
public class Role(string value) : StringEnum(value)
{
    public static readonly Role Bot = new("bot");
    public bool IsBot => Bot.Equals(Value);

    public static readonly Role User = new("user");
    public bool IsUser => User.Equals(Value);

    public static readonly Role AgenticInstance = new("agenticInstance");
    public bool IsAgenticInstance => AgenticInstance.Equals(Value);

    public static readonly Role AgenticUser = new("agenticUser");
    public bool IsAgenticUser => AgenticUser.Equals(Value);
}