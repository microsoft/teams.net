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

    [JsonPropertyName("properties")]
    [JsonPropertyOrder(5)]
    public Dictionary<string, object>? Properties { get; set; }
}

/// <summary>
/// Represents a Teams channel account, extending the basic channel account with Teams-specific properties.
/// This is used to represent a user or bot in Microsoft Teams conversations.
/// </summary>
/// <see href="https://learn.microsoft.com/en-us/dotnet/api/microsoft.bot.schema.teams.teamschannelaccount"/>
public class TeamsChannelAccount
{
    /// <summary>
    /// Unique identifier for the user or bot in the channel.
    /// </summary>
    [JsonPropertyName("id")]
    [JsonPropertyOrder(0)]
    public required string Id { get; set; }

    /// <summary>
    /// Display-friendly name of the user or bot.
    /// </summary>
    [JsonPropertyName("name")]
    [JsonPropertyOrder(1)]
    public string? Name { get; set; }

    /// <summary>
    /// The user's Object ID in Azure Active Directory (AAD).
    /// </summary>
    [JsonPropertyName("objectId")]
    [JsonPropertyOrder(2)]
    public string? ObjectId { get; set; }

    /// <summary>
    /// Role of the user (e.g., 'user' or 'bot').
    /// </summary>
    [JsonPropertyName("role")]
    [JsonPropertyOrder(3)]
    public Role? Role { get; set; }

    /// <summary>
    /// Given name (first name) of the user.
    /// </summary>
    [JsonPropertyName("givenName")]
    [JsonPropertyOrder(4)]
    public string? GivenName { get; set; }

    /// <summary>
    /// Surname (last name) of the user.
    /// </summary>
    [JsonPropertyName("surname")]
    [JsonPropertyOrder(5)]
    public string? Surname { get; set; }

    /// <summary>
    /// Email address of the user.
    /// </summary>
    [JsonPropertyName("email")]
    [JsonPropertyOrder(6)]
    public string? Email { get; set; }

    /// <summary>
    /// Unique User Principal Name (UPN) for the user in AAD.
    /// </summary>
    [JsonPropertyName("userPrincipalName")]
    [JsonPropertyOrder(7)]
    public string? UserPrincipalName { get; set; }

    /// <summary>
    /// Unique identifier for the user's Azure AD tenant.
    /// </summary>
    [JsonPropertyName("tenantId")]
    [JsonPropertyOrder(8)]
    public string? TenantId { get; set; }

    /// <summary>
    /// Custom properties associated with the account.
    /// </summary>
    [JsonPropertyName("properties")]
    [JsonPropertyOrder(9)]
    public Dictionary<string, object>? Properties { get; set; }
}

[JsonConverter(typeof(JsonConverter<Role>))]
public class Role(string value) : StringEnum(value)
{
    public static readonly Role Bot = new("bot");
    public bool IsBot => Bot.Equals(Value);

    public static readonly Role User = new("user");
    public bool IsUser => User.Equals(Value);
}