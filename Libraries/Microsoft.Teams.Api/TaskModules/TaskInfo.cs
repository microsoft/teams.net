// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Api.TaskModules;

/// <summary>
/// This can be a number, representing the task
/// module's height/width in pixels, or a string, one of: small, medium, large.
/// </summary>
[JsonConverter(typeof(JsonConverter<Size>))]
public partial class Size(string value) : StringEnum(value)
{
    public static readonly Size Small = new("small");
    public bool IsSmall => Small.Equals(Value);

    public static readonly Size Medium = new("medium");
    public bool IsMedium => Medium.Equals(Value);

    public static readonly Size Large = new("large");
    public bool IsLarge => Large.Equals(Value);
}

/// <summary>
/// Metadata for a Task Module.
/// </summary>
public class TaskInfo
{
    /// <summary>
    /// Appears below the app name and to the right of the app icon.
    /// </summary>
    [JsonPropertyName("title")]
    [JsonPropertyOrder(0)]
    public string? Title { get; set; }

    /// <summary>
    /// This can be a number, representing the task
    /// module's height in pixels, or a string, one of: small, medium, large.
    /// </summary>
    [JsonPropertyName("height")]
    [JsonPropertyOrder(1)]
    public IUnion<int, Size>? Height { get; set; }

    /// <summary>
    /// This can be a number, representing the task
    /// module's width in pixels, or a string, one of: small, medium, large.
    /// </summary>
    [JsonPropertyName("width")]
    [JsonPropertyOrder(2)]
    public IUnion<int, Size>? Width { get; set; }

    /// <summary>
    /// The URL of what is loaded as an iframe inside the
    /// task module. One of url or card is required.
    /// </summary>
    [JsonPropertyName("url")]
    [JsonPropertyOrder(3)]
    public string? Url { get; set; }

    /// <summary>
    /// The JSON for the Adaptive card to appear in
    /// the task module.
    /// </summary>
    [JsonPropertyName("card")]
    [JsonPropertyOrder(4)]
    public Attachment? Card { get; set; }

    /// <summary>
    /// If a client does not support the task
    /// module feature, this URL is opened in a browser tab.
    /// </summary>
    [JsonPropertyName("fallbackUrl")]
    [JsonPropertyOrder(5)]
    public string? FallbackUrl { get; set; }

    /// <summary>
    /// If a client does not support the task
    /// module feature, this URL is opened in a browser tab.
    /// </summary>
    [JsonPropertyName("completionBotId")]
    [JsonPropertyOrder(6)]
    public string? CompletionBotId { get; set; }
}