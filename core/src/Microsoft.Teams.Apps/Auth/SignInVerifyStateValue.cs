// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Teams.Apps.Auth;

/// <summary>
/// Value payload of the signin/verifyState invoke activity.
/// </summary>
public class SignInVerifyStateValue
{
    /// <summary>
    /// The magic code (state) from the fallback sign-in flow.
    /// </summary>
    [JsonPropertyName("state")]
    public string? State { get; set; }
}
