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
}
