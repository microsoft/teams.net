// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Teams.Bot.Apps.Schema.Invokes;

/// <summary>
/// Task module invoke request value payload.
/// </summary>
public class TaskModuleRequest
{
    /// <summary>
    /// User input data. Free payload with key-value pairs.
    /// </summary>
    [JsonPropertyName("data")]
    public object? Data { get; set; }

    /// <summary>
    /// Current user context, i.e., the current theme.
    /// </summary>
    [JsonPropertyName("context")]
    public TaskModuleRequestContext? Context { get; set; }
}

/// <summary>
/// Current user context, i.e., the current theme.
/// </summary>
public class TaskModuleRequestContext
{
    /// <summary>
    /// The user's current theme.
    /// </summary>
    [JsonPropertyName("theme")]
    public string? Theme { get; set; }
}
