// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Core.Activities;

/// <summary>
/// Represents a typing indicator activity.
/// </summary>
public class TypingActivity : Activity
{
    /// <summary>
    /// Gets or sets the text content of the typing indicator.
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TypingActivity"/> class.
    /// </summary>
    public TypingActivity() : base(ActivityTypes.Typing)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TypingActivity"/> class with the specified text.
    /// </summary>
    /// <param name="text">The text content.</param>
    public TypingActivity(string? text) : base(ActivityTypes.Typing)
    {
        Text = text;
    }
}
