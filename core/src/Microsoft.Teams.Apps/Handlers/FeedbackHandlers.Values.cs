// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Microsoft.Teams.Apps;

/// <summary>
/// Defines the structure that arrives in the Activity.Value for an Invoke activity with
/// Name of 'message/fetchTask'. Sent when the user clicks a feedback button (like/dislike)
/// on an AI-generated message.
/// </summary>
public class MessageFetchTaskInvokeValue
{
    /// <summary>
    /// The data payload containing action name and value.
    /// </summary>
    [JsonPropertyName("data")]
    public MessageFetchTaskData? Data { get; internal set; }
}

/// <summary>
/// The data payload nested inside the fetch task value.
/// </summary>
public class MessageFetchTaskData
{
    /// <summary>
    /// The name of the action.
    /// </summary>
    [JsonPropertyName("actionName")]
    public string? ActionName { get; internal set; }

    /// <summary>
    /// Contains the user's reaction.
    /// </summary>
    [JsonPropertyName("actionValue")]
    public MessageFetchTaskActionValue? ActionValue { get; internal set; }
}

/// <summary>
/// The nested action value containing the user's reaction.
/// </summary>
public class MessageFetchTaskActionValue
{
    /// <summary>
    /// The feedback button the user clicked. Either "like" or "dislike".
    /// </summary>
    [JsonPropertyName("reaction")]
    public string? Reaction { get; internal set; }
}

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
    public JsonNode? ActionValue { get; set; }
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
    public string? Reaction { get; set; }

    /// <summary>
    /// The user's response, as a JSON-encoded string containing the form input values
    /// (e.g. <c>{"feedbackText":"..."}</c>). Parse with <see cref="System.Text.Json.JsonDocument.Parse(string, System.Text.Json.JsonDocumentOptions)"/> to read individual fields.
    /// </summary>
    [JsonPropertyName("feedback")]
    public string? Feedback { get; set; }
}
