// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Microsoft.Teams.Bot.Apps.Schema.Entities;

namespace Microsoft.Teams.Bot.Apps.Schema;

/// <summary>
/// Represents a streaming activity chunk. Has type "typing" to satisfy the Teams
/// streaming API, but carries text content that accumulates into the final response.
/// </summary>
public class StreamingActivity : TeamsActivity
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StreamingActivity"/> class with the specified text.
    /// </summary>
    /// <param name="text"></param>
    [JsonConstructor]
    public StreamingActivity(string text) : base(TeamsActivityType.Typing)
    {
        Text = text;
        StreamInfo = new StreamInfoEntity();
        Entities ??= [];
        Entities.Add(StreamInfo);
    }

    /// <summary>
    /// Gets or sets the text content of the streaming chunk.
    /// </summary>
    [JsonPropertyName("text")]
    public string Text { get; set; }

    /// <summary>
    /// Gets the stream info entity for this streaming activity.
    /// </summary>
    public StreamInfoEntity StreamInfo { get; }

    /// <summary>
    /// Creates a new <see cref="StreamingActivityBuilder"/> with an initial text chunk.
    /// </summary>
    public static StreamingActivityBuilder CreateBuilder(string text = "") => new(text);
}
