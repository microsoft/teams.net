// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Teams.Apps;

/// <summary>
/// Defines the structure that arrives in the Activity.Value for Invoke activity with
/// Name of 'adaptiveCard/action'.
/// </summary>
public class AdaptiveCardActionValue
{
    /// <summary>
    /// The action of this adaptive card invoke action value.
    /// </summary>
    [JsonPropertyName("action")]
    public AdaptiveCardAction? Action { get; internal set; }

    /// <summary>
    /// The state for this adaptive card invoke action value.
    /// </summary>
    [JsonPropertyName("state")]
    public string? State { get; internal set; }

    /// <summary>
    /// What triggered the action.
    /// </summary>
    [JsonPropertyName("trigger")]
    public string? Trigger { get; internal set; }
}

/// <summary>
/// Defines the structure that arrives in the Activity.Value.Action for Invoke
/// activity with Name of 'adaptiveCard/action'.
/// </summary>
public class AdaptiveCardAction
{
    /// <summary>
    /// The Type of this Adaptive Card Invoke Action.
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; internal set; }

    /// <summary>
    /// The id of this Adaptive Card Invoke Action.
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; internal set; }

    /// <summary>
    /// The title of this Adaptive Card Invoke Action.
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; internal set; }

    /// <summary>
    /// The Verb of this adaptive card action invoke.
    /// </summary>
    [JsonPropertyName("verb")]
    public string? Verb { get; internal set; }

    /// <summary>
    /// The Data of this adaptive card action invoke.
    /// </summary>
    [JsonPropertyName("data")]
    public Dictionary<string, object>? Data { get; internal set; }
}
