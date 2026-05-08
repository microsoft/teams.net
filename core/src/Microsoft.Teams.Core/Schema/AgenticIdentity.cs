// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Teams.Core.Schema;

/// <summary>
/// Represents an agentic identity for user-delegated token acquisition.
/// </summary>
public sealed class AgenticIdentity
{
    /// <summary>
    /// Agentic application ID.
    /// </summary>
    public string? AgenticAppId { get; set; }
    /// <summary>
    /// Agentic user ID.
    /// </summary>
    public string? AgenticUserId { get; set; }

    /// <summary>
    /// Agentic application blueprint ID.
    /// </summary>
    public string? AgenticAppBlueprintId { get; set; }

    /// <summary>
    /// Creates an <see cref="AgenticIdentity"/> from a <see cref="ConversationAccount"/>'s typed agentic fields.
    /// Returns null if the account is null or has no agentic fields set.
    /// </summary>
    public static AgenticIdentity? FromAccount(ConversationAccount? account)
    {
        if (account is null || (account.AgenticAppId is null && account.AgenticUserId is null && account.AgenticAppBlueprintId is null))
        {
            return null;
        }

        return new AgenticIdentity
        {
            AgenticAppId = account.AgenticAppId,
            AgenticUserId = account.AgenticUserId,
            AgenticAppBlueprintId = account.AgenticAppBlueprintId
        };
    }
}
