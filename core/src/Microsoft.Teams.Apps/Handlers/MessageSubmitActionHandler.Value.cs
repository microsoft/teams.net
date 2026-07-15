// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Microsoft.Teams.Apps.Handlers;

/// <summary>
/// Defines the structure that arrives in the Activity.Value for an Invoke activity with
/// Name of 'message/submitAction'.
/// </summary>
public class SubmitActionValue
{
    /// <summary>
    /// The name of the action that was submitted.
    /// </summary>
    [JsonPropertyName("actionName")]
    public required string ActionName { get; set; }

    /// <summary>
    /// The data submitted with the action.
    /// </summary>
    [JsonPropertyName("actionValue")]
    public JsonNode? ActionValue { get; internal set; }
}

/// <summary>
/// Strongly-typed shape of <see cref="SubmitActionValue.ActionValue"/> when
/// <see cref="SubmitActionValue.ActionName"/> is <c>"feedback"</c> — i.e. when the user
/// submits a custom feedback form. Mirrors the payload Teams sends after the user
/// clicks Submit on the bot's feedback task module.
/// </summary>
public class MessageSubmitFeedbackValue
{
    /// <summary>
    /// The reaction the user clicked. Typically <c>"like"</c> or <c>"dislike"</c>.
    /// </summary>
    [JsonPropertyName("reaction")]
    public string? Reaction { get; internal set; }

    /// <summary>
    /// The user's response, as a JSON-encoded string containing the form input values
    /// (e.g. <c>{"feedbackText":"..."}</c>). Parse with <c>JsonDocument.Parse</c> to read individual fields.
    /// </summary>
    [JsonPropertyName("feedback")]
    public string? Feedback { get; internal set; }
}
