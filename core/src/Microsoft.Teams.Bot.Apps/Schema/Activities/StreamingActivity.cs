// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Microsoft.Teams.Bot.Core.Schema;

namespace Microsoft.Teams.Bot.Apps.Schema;

/// <summary>
/// Represents a streaming activity chunk. Has type "typing" to satisfy the Teams
/// streaming API, but carries text content that accumulates into the final response.
/// </summary>
public class StreamingActivity : TeamsActivity
{
    /// <summary>
    /// Default constructor.
    /// </summary>
    [JsonConstructor]
    public StreamingActivity() : base(TeamsActivityType.Typing)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StreamingActivity"/> class with the specified text.
    /// </summary>
    /// <param name="text"></param>
    public StreamingActivity(string text) : base(TeamsActivityType.Typing)
    {
        Text = text;
    }

    /// <summary>
    /// Gets or sets the text content of the streaming chunk.
    /// </summary>
    [JsonPropertyName("text")]
    public string? Text { get; set; }
}
