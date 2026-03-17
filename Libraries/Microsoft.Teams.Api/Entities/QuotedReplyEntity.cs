// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Teams.Api.Entities;

public class QuotedReplyEntity : Entity
{
    [JsonPropertyName("quotedReply")]
    [JsonPropertyOrder(3)]
    public QuotedReplyData? QuotedReply { get; set; }

    public QuotedReplyEntity() : base("quotedReply") { }
}

public class QuotedReplyData
{
    [JsonPropertyName("messageId")]
    public string? MessageId { get; set; }

    [JsonPropertyName("senderId")]
    public string? SenderId { get; set; }

    [JsonPropertyName("senderName")]
    public string? SenderName { get; set; }

    [JsonPropertyName("preview")]
    public string? Preview { get; set; }

    [JsonPropertyName("time")]
    public string? Time { get; set; }

    [JsonPropertyName("isReplyDeleted")]
    public bool? IsReplyDeleted { get; set; }

    [JsonPropertyName("validatedMessageReference")]
    public bool? ValidatedMessageReference { get; set; }
}