// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
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
    [Obsolete("Use Account.Type instead (e.g., AccountType.Person, AccountType.Bot). Will be removed by end of summer 2026.")]
    public Role? Role { get; set; }

    /// <summary>
    /// The type of the account. Possible values: 'person', 'bot', 'channel', 'team', 'tag'.
    /// Primarily present on mention entities for non-person accounts. Absent for regular person accounts.
    /// </summary>
    [JsonPropertyName("type")]
    [JsonPropertyOrder(3)]
    public AccountType? Type { get; set; }

    [JsonPropertyName("name")]
    [JsonPropertyOrder(4)]
    public string? Name { get; set; }

    [JsonPropertyName("membershipSources")]
    [JsonPropertyOrder(5)]
    public IList<MembershipSource>? MembershipSources { get; set; }

    [JsonPropertyName("properties")]
    [JsonPropertyOrder(6)]
    public Dictionary<string, object>? Properties { get; set; }

    [JsonPropertyName("isTargeted")]
    [JsonPropertyOrder(7)]
    [JsonInclude]
    [Experimental("ExperimentalTeamsTargeted")]
    public bool? IsTargeted { get; internal set; }
}

[JsonConverter(typeof(JsonConverter<Role>))]
public class Role(string value) : StringEnum(value)
{
    public static readonly Role Bot = new("bot");
    public bool IsBot => Bot.Equals(Value);

    public static readonly Role User = new("user");
    public bool IsUser => User.Equals(Value);
}

[JsonConverter(typeof(JsonConverter<AccountType>))]
public class AccountType(string value) : StringEnum(value)
{
    public static readonly AccountType Person = new("person");
    public bool IsPerson => Person.Equals(Value);

    public static readonly AccountType Bot = new("bot");
    public bool IsBot => Bot.Equals(Value);

    public static readonly AccountType Channel = new("channel");
    public bool IsChannel => Channel.Equals(Value);

    public static readonly AccountType Team = new("team");
    public bool IsTeam => Team.Equals(Value);

    public static readonly AccountType Tag = new("tag");
    public bool IsTag => Tag.Equals(Value);
}