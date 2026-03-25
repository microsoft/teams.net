// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Microsoft.Teams.Bot.Apps.Schema.Entities;

/// <summary>
/// Represents a quoted reply entity in a Teams activity.
/// </summary>
[Experimental("ExperimentalTeamsQuotedReplies")]
public class QuotedReplyEntity : Entity
{
    /// <summary>
    /// Creates a new instance of <see cref="QuotedReplyEntity"/>.
    /// </summary>
    public QuotedReplyEntity() : base("quotedReply") { }

    /// <summary>
    /// Creates a new instance of <see cref="QuotedReplyEntity"/> with the specified message ID.
    /// </summary>
    /// <param name="messageId">The ID of the message being quoted.</param>
    public QuotedReplyEntity(string messageId) : base("quotedReply")
    {
        QuotedReply = new QuotedReplyData { MessageId = messageId };
    }

    /// <summary>
    /// Gets or sets the quoted reply data.
    /// </summary>
    [JsonPropertyName("quotedReply")]
    public QuotedReplyData? QuotedReply
    {
        get => base.Properties.TryGetValue("quotedReply", out object? value) ? value as QuotedReplyData : null;
        set => base.Properties["quotedReply"] = value;
    }
}

/// <summary>
/// Data for a quoted reply entity.
/// </summary>
[Experimental("ExperimentalTeamsQuotedReplies")]
public class QuotedReplyData
{
    /// <summary>
    /// The ID of the quoted message. Required.
    /// </summary>
    [JsonPropertyName("messageId")]
    public string MessageId { get; set; } = string.Empty;

    /// <summary>
    /// The sender's bot-framework ID. Absent for deleted quotes.
    /// </summary>
    [JsonPropertyName("senderId")]
    public string? SenderId { get; set; }

    /// <summary>
    /// The sender's display name. Absent for deleted quotes and TFL senders.
    /// </summary>
    [JsonPropertyName("senderName")]
    public string? SenderName { get; set; }

    /// <summary>
    /// Preview of the quoted message text. Absent for deleted quotes and adaptive cards.
    /// </summary>
    [JsonPropertyName("preview")]
    public string? Preview { get; set; }

    /// <summary>
    /// Timestamp of the quoted message. Absent for deleted quotes.
    /// </summary>
    [JsonPropertyName("time")]
    public string? Time { get; set; }

    /// <summary>
    /// Whether the quoted message was deleted. Omitted when false.
    /// </summary>
    [JsonPropertyName("isReplyDeleted")]
    public bool? IsReplyDeleted { get; set; }

    /// <summary>
    /// Whether all quoted message references are valid and compliant. Only included when true.
    /// </summary>
    [JsonPropertyName("validatedMessageReference")]
    public bool? ValidatedMessageReference { get; set; }
}
