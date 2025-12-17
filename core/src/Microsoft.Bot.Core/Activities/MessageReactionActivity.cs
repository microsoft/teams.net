// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;

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
    public MessageUpdateActivity(string? text) : base(text)
    {
        Type = ActivityTypes.MessageUpdate;
    }
}

/// <summary>
/// Represents a reaction in a message.
/// </summary>
public class MessageReaction
{
    /// <summary>
    /// Gets or sets the type of reaction.
    /// </summary>
    public string? Type { get; set; }
}

/// <summary>
/// Represents a message reaction activity.
/// </summary>
public class MessageReactionActivity : Activity
{
    /// <summary>
    /// Gets or sets the reactions that were added.
    /// </summary>
    [SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Matches source API structure for serialization")]
    [SuppressMessage("Design", "CA1002:Do not expose generic lists", Justification = "Matches source API structure")]
    public List<MessageReaction>? ReactionsAdded { get; set; }

    /// <summary>
    /// Gets or sets the reactions that were removed.
    /// </summary>
    [SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Matches source API structure for serialization")]
    [SuppressMessage("Design", "CA1002:Do not expose generic lists", Justification = "Matches source API structure")]
    public List<MessageReaction>? ReactionsRemoved { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageReactionActivity"/> class.
    /// </summary>
    public MessageReactionActivity() : base(ActivityTypes.MessageReaction)
    {
    }
}
