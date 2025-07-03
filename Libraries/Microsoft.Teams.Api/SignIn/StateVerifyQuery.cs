// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Teams.Api.SignIn;

/// <summary>
/// Signin state (part of signin action auth flow) verification invoke query
/// </summary>
public class StateVerifyQuery
{
    /// <summary>
    /// The state string originally received when the
    /// signin web flow is finished with a state posted back to client via tab SDK
    /// microsoftTeams.authentication.notifySuccess(state)
    /// </summary>
    [JsonPropertyName("state")]
    [JsonPropertyOrder(0)]
    public string? State { get; set; }
}