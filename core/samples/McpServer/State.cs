// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;

namespace McpServer;

public static class AskStatus
{
    public const string Pending = "pending";
    public const string Answered = "answered";
    public const string Superseded = "superseded";
}

public static class ApprovalStatus
{
    public const string Pending = "pending";
    public const string Approved = "approved";
    public const string Rejected = "rejected";
}

// Immutable so readers always see a consistent (Status, Reply) snapshot — no locks,
// no Volatile, no torn state. Transitions go through ConcurrentDictionary.TryUpdate
// in State.PendingAsks: build a new record, swap atomically against the old one.
public sealed record PendingAsk(string UserId, string Status = AskStatus.Pending, string? Reply = null);

/// <summary>
/// In-memory state shared between the Teams bot handlers and the MCP tools.
/// A server restart clears everything — pending asks and approvals in flight will be lost.
/// </summary>
public sealed class State
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
    /// Service URL used by proactive sends and <c>conversations.create</c>. Updated from
    /// the first incoming activity; falls back to the default Teams endpoint until then.
    /// </summary>
    public Uri ServiceUrl { get; set; } = new Uri("https://smba.trafficmanager.net/teams/");
}
