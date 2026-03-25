// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Microsoft.Teams.Api.Entities;

[Experimental("ExperimentalTeamsQuotedReplies")]
public class QuotedReplyEntity : Entity
{
    [JsonPropertyName("quotedReply")]
    [JsonPropertyOrder(3)]
    public required QuotedReplyData QuotedReply { get; set; }

    public QuotedReplyEntity() : base("quotedReply") { }
}

[Experimental("ExperimentalTeamsQuotedReplies")]
public class QuotedReplyData
{
    [JsonPropertyName("messageId")]
    public required string MessageId { get; set; }

    [JsonPropertyName("senderId")]
    public string? SenderId { get; set; }

    [JsonPropertyName("senderName")]
    public string? SenderName { get; set; }

    [JsonPropertyName("preview")]
    public string? Preview { get; set; }

    /// <summary>
    /// Timestamp of the quoted message (IC3 epoch value, e.g. "1772050244572").
    /// Populated on inbound; ignored on outbound. Absent for deleted quotes.
    /// </summary>
    [JsonPropertyName("time")]
    public string? Time { get; set; }

    [JsonPropertyName("isReplyDeleted")]
    public bool? IsReplyDeleted { get; set; }

    [JsonPropertyName("validatedMessageReference")]
    public bool? ValidatedMessageReference { get; set; }
}