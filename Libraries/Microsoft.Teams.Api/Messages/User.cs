// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Api.Messages;

/// <summary>
/// The identity type of the
/// user. Possible values include: 'aadUser', 'onPremiseAadUser',
/// 'anonymousGuest', 'federatedUser'
/// </summary>
[JsonConverter(typeof(JsonConverter<UserIdentityType>))]
public class UserIdentityType(string value) : StringEnum(value)
{
    public static readonly UserIdentityType AadUser = new("aadUser");
    public bool IsAadUser => AadUser.Equals(Value);

    public static readonly UserIdentityType OnPremiseAadUser = new("onPremiseAadUser");
    public bool IsOnPremiseAadUser => OnPremiseAadUser.Equals(Value);

    public static readonly UserIdentityType AnonymousGuest = new("anonymousGuest");
    public bool IsAnonymousGuest => AnonymousGuest.Equals(Value);

    public static readonly UserIdentityType FederatedUser = new("federatedUser");
    public bool IsFederatedUser => FederatedUser.Equals(Value);
}

/// <summary>
/// Represents a user entity.
/// </summary>
public class User
{
    /// <summary>
    /// The id of the user.
    /// </summary>
    [JsonPropertyName("id")]
    [JsonPropertyOrder(0)]
    public required string Id { get; set; }

    /// <summary>
    /// The identity type of the
    /// user. Possible values include: 'aadUser', 'onPremiseAadUser',
    /// 'anonymousGuest', 'federatedUser'
    /// </summary>
    [JsonPropertyName("userIdentityType")]
    [JsonPropertyOrder(1)]
    public UserIdentityType? UserIdentityType { get; set; }

    /// <summary>
    /// The plaintext display name of the user.
    /// </summary>
    [JsonPropertyName("displayName")]
    [JsonPropertyOrder(2)]
    public string? DisplayName { get; set; }
}