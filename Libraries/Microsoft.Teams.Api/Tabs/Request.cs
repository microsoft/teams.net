// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Teams.Api.Tabs;

/// <summary>
/// Invoke ('tab/fetch') request value payload.
/// </summary>
public class Request
{
    /// <summary>
    /// The current tab entity request context.
    /// </summary>
    [JsonPropertyName("tabContext")]
    [JsonPropertyOrder(0)]
    public EntityContext? TabContext { get; set; }

    /// <summary>
    /// The current user context, i.e., the current theme.
    /// </summary>
    [JsonPropertyName("context")]
    [JsonPropertyOrder(1)]
    public Context? Context { get; set; }

    /// <summary>
    /// The magic code for OAuth flow.
    /// </summary>
    [JsonPropertyName("state")]
    [JsonPropertyOrder(2)]
    public string? State { get; set; }
}