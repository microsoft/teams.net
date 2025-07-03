// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Teams.Api.AdaptiveCards;

/// <summary>
/// Defines the structure that arrives in the Activity.Value.Action for Invoke
/// activity with Name of 'adaptiveCard/action'.
/// </summary>
public class InvokeAction
{
    /// <summary>
    /// The Type of this Adaptive Card Invoke Action.
    /// </summary>
    [JsonPropertyName("type")]
    [JsonPropertyOrder(0)]
    public required ActionType Type { get; set; }

    /// <summary>
    /// The id of this Adaptive Card Invoke Action.
    /// </summary>
    [JsonPropertyName("id")]
    [JsonPropertyOrder(1)]
    public string? Id { get; set; }

    /// <summary>
    /// The Verb of this adaptive card action invoke.
    /// </summary>
    [JsonPropertyName("verb")]
    [JsonPropertyOrder(2)]
    public string? Verb { get; set; }

    /// <summary>
    /// The Data of this adaptive card action invoke.
    /// </summary>
    [JsonPropertyName("data")]
    [JsonPropertyOrder(3)]
    public IDictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
}