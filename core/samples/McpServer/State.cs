// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;

namespace McpServer;

public static class AskStatus
{
    public const string Pending = "pending";
    public const string Answered = "answered";
}

public static class ApprovalStatus
{
    public const string Pending = "pending";
    public const string Approved = "approved";
    public const string Rejected = "rejected";
}

public sealed class PendingAsk
{
    public required string UserId { get; init; }
    public string Status { get; set; } = AskStatus.Pending;
    public string? Reply { get; set; }
}

/// <summary>
/// In-memory state shared between the Teams bot handlers and the MCP tools.
/// A server restart clears everything — pending asks and approvals in flight will be lost.
/// </summary>
public sealed class State(Uri serviceUrl)
{
    /// <summary>userId -> personal conversationId. Populated on first incoming 1:1 message.</summary>
    public ConcurrentDictionary<string, string> Conversations { get; } = new();

    /// <summary>requestId -> PendingAsk.</summary>
    public ConcurrentDictionary<string, PendingAsk> PendingAsks { get; } = new();

    /// <summary>userId -> requestId for their current pending ask. Cleared once the user replies.</summary>
    public ConcurrentDictionary<string, string> UserPendingAsk { get; } = new();

    /// <summary>approvalId -> approval status. Values: "pending", "approved", "rejected".</summary>
    public ConcurrentDictionary<string, string> Approvals { get; } = new();

    /// <summary>
    /// Service URL used by proactive sends and <c>conversations.create</c>. Seeded from
    /// <c>Bot:ServiceUrl</c> config at startup.
    /// </summary>
    public Uri ServiceUrl { get; set; } = serviceUrl;
}
