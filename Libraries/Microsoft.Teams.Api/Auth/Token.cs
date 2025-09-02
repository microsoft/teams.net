// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Teams.Api.Auth;

/// <summary>
/// any authorized token
/// </summary>
public interface IToken
{
    /// <summary>
    /// the app id
    /// </summary>
    public string? AppId { get; }

    /// <summary>
    /// the app display name
    /// </summary>
    public string? AppDisplayName { get; }

    /// <summary>
    /// the tenant id
    /// </summary>
    public string? TenantId { get; }

    /// <summary>
    /// the service url to send responses to
    /// </summary>
    public string ServiceUrl { get; }

    /// <summary>
    /// where the activity originated from
    /// </summary>
    public CallerType From { get; }

    /// <summary>
    /// the id of the acitivity sender
    /// </summary>
    public string FromId { get; }

    /// <summary>
    /// the timestamp when this token expires
    /// </summary>
    public DateTime? Expiration { get; }

    /// <summary>
    /// check if the token is expired
    /// </summary>
    /// <returns>true if expired, otherwise false</returns>
    public bool IsExpired();

    /// <summary>
    /// convert the token to its string representation
    /// </summary>
    public string ToString();
}

[JsonConverter(typeof(JsonConverter<CallerType>))]
public class CallerType(string value) : Common.StringEnum(value)
{
    public static readonly CallerType Bot = new("bot");
    public bool IsBot => Bot.Equals(Value);

    public static readonly CallerType Azure = new("azure");
    public bool IsAzure => Azure.Equals(Value);

    public static readonly CallerType Gov = new("gov");
    public bool IsGov => Gov.Equals(Value);
}