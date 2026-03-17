// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Microsoft.Teams.Bot.Core.Schema;

namespace Microsoft.Teams.Bot.Apps.Schema;

/// <summary>
/// Represents a typing activity used to send intermediate streaming updates.
/// </summary>
public class TypingActivity : TeamsActivity
{

    /// <summary>
    /// Default constructor.
    /// </summary>
    [JsonConstructor]
    public TypingActivity() : base(TeamsActivityType.Typing)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TypingActivity"/> class with the specified text.
    /// </summary>
    /// <param name="text">The accumulated text content of the streaming chunk.</param>
    public TypingActivity(string text) : base(TeamsActivityType.Typing)
    {
        Text = text;
    }

    /// <summary>
    /// Internal constructor to create TypingActivity from CoreActivity.
    /// </summary>
    protected TypingActivity(CoreActivity activity) : base(activity)
    {
        if (activity.Properties.TryGetValue("text", out object? text))
        {
            Text = text?.ToString();
            activity.Properties.Remove("text");
        }
    }

    /// <summary>
    /// Gets or sets the accumulated text content of the streaming chunk.
    /// </summary>
    [JsonPropertyName("text")]
    public string? Text { get; set; }
}
