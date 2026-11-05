// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Microsoft.Teams.Apps;

/// <summary>Serializable prompt binding for a Plan. Supply the runtime API client separately.</summary>
[Experimental("ExperimentalTeamsPlan")]
public sealed class PlanOptions
{
    /// <summary>The full conversation ID.</summary>
    [JsonPropertyName("conversationId")]
    public required string ConversationId { get; init; }

    /// <summary>The originating prompt's activity ID, not a later event or response ID.</summary>
    [JsonPropertyName("replyToId")]
    public required string ReplyToId { get; init; }

    /// <summary>
    /// The reply chain (thread) of a chat that <see cref="Plan.CompleteAsync(string, CancellationToken)"/> posts to, as <c>ctx.send</c> does.
    /// Omit it when <see cref="ConversationId"/> already identifies the thread, as in a channel, or when the prompt is not inside a thread.
    /// </summary>
    [JsonPropertyName("replyChain")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public PlanReplyChain? ReplyChain { get; init; }
}

/// <summary>The reply chain (thread) of a chat that a Plan completion is posted to, taken from the inbound <c>channelData</c>.</summary>
[Experimental("ExperimentalTeamsPlan")]
public sealed class PlanReplyChain
{
    /// <summary>The chat id, <c>channelData.chat.id</c>.</summary>
    [JsonPropertyName("chatOrChannelId")]
    public required string ChatOrChannelId { get; init; }

    /// <summary>The id of the message at the root of the reply chain, <c>channelData.replyChain.id</c>.</summary>
    [JsonPropertyName("replyChainId")]
    public required string ReplyChainId { get; init; }
}
