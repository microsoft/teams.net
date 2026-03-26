// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Teams.Bot.Apps.Schema;

/// <summary>
/// Provides a fluent API for building <see cref="StreamingActivity"/> instances.
/// </summary>
public class StreamingActivityBuilder : TeamsActivityBuilder<StreamingActivity, StreamingActivityBuilder>
{
    /// <summary>
    /// Initializes a new instance of the StreamingActivityBuilder class with an initial text chunk.
    /// </summary>
    /// <param name="text">The initial text content of the streaming chunk.</param>
    internal StreamingActivityBuilder(string text = "") : base(new StreamingActivity(text))
    {
    }

    /// <summary>
    /// Sets the text content of the streaming chunk.
    /// </summary>
    public StreamingActivityBuilder WithText(string text)
    {
        _activity.Text = text;
        return this;
    }

    /// <summary>
    /// Builds and returns the configured StreamingActivity instance.
    /// </summary>
    public override StreamingActivity Build()
    {
        _activity.Rebase();
        return _activity;
    }
}
