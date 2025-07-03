// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Teams.Api.Tabs;

/// <summary>
/// Invoke ('tab/submit') request value payload.
/// </summary>
public class Submit
{
    /// <summary>
    /// The current tab's entity request context.
    /// </summary>
    [JsonPropertyName("tabContext")]
    [JsonPropertyOrder(0)]
    public EntityContext? TabContext { get; set; }

    /// <summary>
    /// The current user context, i.e., the current theme.
    /// </summary>
    [JsonPropertyName("context")]
    [JsonPropertyOrder(1)]
    public Context? Context { get; set; }

    /// <summary>
    /// User input. Free payload containing properties of key-value pairs.
    /// </summary>
    [JsonPropertyName("data")]
    [JsonPropertyOrder(2)]
    public SubmitData? Data { get; set; }
}

/// <summary>
/// Invoke ('tab/submit') request value payload data.
/// </summary>
public class SubmitData
{
    /// <summary>
    /// Should currently be 'tab/submit'.
    /// </summary>
    [JsonPropertyName("type")]
    [JsonPropertyOrder(0)]
    public string? Type { get; set; }

    /// <summary>
    /// other
    /// </summary>
    [JsonExtensionData]
    public IDictionary<string, object?> Properties { get; set; } = new Dictionary<string, object?>();
}