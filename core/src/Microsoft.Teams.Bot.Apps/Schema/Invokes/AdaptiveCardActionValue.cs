// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Teams.Bot.Apps.Schema;

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
    public AdaptiveCardAction? Action { get; set; }

    /// <summary>
    /// The state for this adaptive card invoke action value.
    /// </summary>
    [JsonPropertyName("state")]
    public string? State { get; set; }

    /// <summary>
    /// What triggered the action.
    /// </summary>
    [JsonPropertyName("trigger")]
    public string? Trigger { get; set; }
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
    public string? Type { get; set; }

    /// <summary>
    /// The id of this Adaptive Card Invoke Action.
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// The title of this Adaptive Card Invoke Action.
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>
    /// The Verb of this adaptive card action invoke.
    /// </summary>
    [JsonPropertyName("verb")]
    public string? Verb { get; set; }

    /// <summary>
    /// The Data of this adaptive card action invoke.
    /// </summary>
    [JsonPropertyName("data")]
    public Dictionary<string, object>? Data { get; set; }
}
