// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Microsoft.Teams.Bot.Apps.Schema;

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
