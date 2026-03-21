// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Teams.Bot.DevTools.Models;

/// <summary>
/// A custom page that can be added to the DevTools UI.
/// </summary>
public class Page
{
    /// <summary>
    /// An optional icon name shown in the view header.
    /// </summary>
    [JsonPropertyName("icon")]
    [JsonPropertyOrder(0)]
    public string? Icon { get; set; }

    /// <summary>
    /// The unique name of the view (must be URL-safe).
    /// </summary>
    [JsonPropertyName("name")]
    [JsonPropertyOrder(1)]
    public required string Name { get; set; }

    /// <summary>
    /// The display name shown in the view header.
    /// </summary>
    [JsonPropertyName("displayName")]
    [JsonPropertyOrder(2)]
    public required string DisplayName { get; set; }

    /// <summary>
    /// The URL of the view.
    /// </summary>
    [JsonPropertyName("url")]
    [JsonPropertyOrder(3)]
    public required Uri Url { get; set; }
}
