// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Teams.Bot.DevTools.Models;

/// <summary>
/// App metadata sent to DevTools UI clients on connection.
/// </summary>
public class DevToolsMetaData
{
    /// <summary>
    /// The bot application ID.
    /// </summary>
    [JsonPropertyName("id")]
    [JsonPropertyOrder(0)]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The bot application name.
    /// </summary>
    [JsonPropertyName("name")]
    [JsonPropertyOrder(1)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Custom pages registered with DevTools.
    /// </summary>
    [JsonPropertyName("pages")]
    [JsonPropertyOrder(2)]
    public IList<Page> Pages { get; } = [];
}
