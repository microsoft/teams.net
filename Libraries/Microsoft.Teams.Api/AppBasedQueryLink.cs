// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Teams.Api;

/// <summary>
/// An interface representing AppBasedLinkQuery.
/// </summary>
public class AppBasedQueryLink
{
    /// <summary>
    /// Url queried by user
    /// </summary>
    [JsonPropertyName("url")]
    [JsonPropertyOrder(0)]
    public string? Url { get; set; }

    /// <summary>
    /// State is the magic code for OAuth Flow
    /// </summary>
    [JsonPropertyName("state")]
    [JsonPropertyOrder(1)]
    public string? State { get; set; }
}