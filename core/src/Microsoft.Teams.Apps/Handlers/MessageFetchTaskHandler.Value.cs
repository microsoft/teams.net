// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Teams.Apps.Handlers;

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
