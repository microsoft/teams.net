// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Teams.Core.Schema;

/// <summary>
/// Represents an agentic user for user-delegated token acquisition.
/// </summary>
public sealed class AgenticUser
{
    /// <summary>
    /// Agentic app instance ID.
    /// </summary>
    public string? AgenticAppInstanceId { get; set; }

    /// <summary>
    /// Agentic user ID.
    /// </summary>
    public string? AgenticUserId { get; set; }

    /// <summary>
    /// Tenant ID associated with the agentic user.
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// Agentic blueprint ID.
    /// </summary>
    public string? AgenticBlueprintId { get; set; }

    /// <summary>
    /// Creates an <see cref="AgenticUser"/> from a <see cref="ChannelAccount"/>'s typed agentic user fields.
    /// Returns null if the account is null or has no agentic user fields set.
    /// </summary>
    public static AgenticUser? FromAccount(ChannelAccount? account)
    {
        if (account is null || (account.AgenticAppInstanceId is null && account.AgenticUserId is null && account.AgenticBlueprintId is null))
        {
            return null;
        }

        return new AgenticUser
        {
            AgenticAppInstanceId = account.AgenticAppInstanceId,
            AgenticUserId = account.AgenticUserId,
            AgenticBlueprintId = account.AgenticBlueprintId,
            TenantId = account.TenantId
        };
    }
}
