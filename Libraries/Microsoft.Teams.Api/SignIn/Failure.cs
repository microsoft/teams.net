// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Teams.Api.SignIn;

/// <summary>
/// Sign-in failure information sent by Teams when SSO token exchange fails.
/// </summary>
public class Failure
{
    /// <summary>
    /// The error code for the sign-in failure (e.g., "resourcematchfailed").
    /// </summary>
    [JsonPropertyName("code")]
    [JsonPropertyOrder(0)]
    public string? Code { get; set; }

    /// <summary>
    /// The error message for the sign-in failure.
    /// </summary>
    [JsonPropertyName("message")]
    [JsonPropertyOrder(1)]
    public string? Message { get; set; }
}
