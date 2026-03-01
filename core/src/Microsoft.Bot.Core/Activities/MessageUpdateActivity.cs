// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Core.Activities;

/// <summary>
/// Represents a message update activity.
/// </summary>
public class MessageUpdateActivity : MessageActivity
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MessageUpdateActivity"/> class.
    /// </summary>
    public MessageUpdateActivity() : base()
    {
        Type = ActivityTypes.MessageUpdate;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageUpdateActivity"/> class with the specified text.
    /// </summary>
    /// <param name="text">The text content of the message.</param>
    public MessageUpdateActivity(string text) : base(text)
    {
        Type = ActivityTypes.MessageUpdate;
    }
}
