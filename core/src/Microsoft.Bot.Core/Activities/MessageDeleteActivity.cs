// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Core.Activities;

/// <summary>
/// Represents a message delete activity.
/// </summary>
public class MessageDeleteActivity : Activity
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MessageDeleteActivity"/> class.
    /// </summary>
    public MessageDeleteActivity() : base(ActivityTypes.MessageDelete)
    {
    }
}
