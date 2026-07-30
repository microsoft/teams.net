// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Teams.Core.Schema;

/// <summary>
/// Identifies the subject kind carried by <see cref="AgenticIdentity"/>.
/// </summary>
public enum AgenticIdentityKind
{
    /// <summary>
    /// The request is scoped to an agentic user.
    /// </summary>
    AgenticUser,

    /// <summary>
    /// The request is scoped to an agentic app instance. No public factory exists until the SDK supports this flow.
    /// </summary>
    AgenticAppInstance
}

/// <summary>
/// Canonical request-scoping carrier for Agent 365 API calls.
/// </summary>
/// <remarks>
/// <see cref="AgenticIdentity"/> represents the agentic program/identity scope used for SDK operations,
/// such as proactive sends, API clients, request options, and token acquisition. It is a discriminated
/// carrier because that operation scope can encompass concrete Agent 365 concepts over time, including
/// <c>AgenticBlueprint</c>, <c>AgenticAppInstance</c>, and <see cref="AgenticUser"/>. <see cref="AgenticUser"/>
/// remains the concrete Teams/activity-facing identity model; convert it to <see cref="AgenticIdentity"/>
/// when scoping SDK operations.
/// </remarks>
public sealed record AgenticIdentity
{
    /// <summary>
    /// Gets the kind of agentic identity.
    /// </summary>
    public AgenticIdentityKind Kind { get; }

    /// <summary>
    /// Gets the agentic app instance ID.
    /// </summary>
    public string AgenticAppInstanceId { get; }

    /// <summary>
    /// Gets the agentic user ID when <see cref="Kind"/> is <see cref="AgenticIdentityKind.AgenticUser"/>.
    /// </summary>
    public string? AgenticUserId { get; }

    /// <summary>
    /// Gets the agentic blueprint ID when supplied by the activity.
    /// </summary>
    public string? AgenticBlueprintId { get; }

    /// <summary>
    /// Gets the tenant ID when supplied by the activity.
    /// </summary>
    public string? TenantId { get; }

    private AgenticIdentity(AgenticIdentityKind kind, string agenticAppInstanceId, string? agenticUserId, string? agenticBlueprintId, string? tenantId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agenticAppInstanceId);

        if (kind == AgenticIdentityKind.AgenticUser)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(agenticUserId);
        }

        Kind = kind;
        AgenticAppInstanceId = agenticAppInstanceId;
        AgenticUserId = agenticUserId;
        AgenticBlueprintId = agenticBlueprintId;
        TenantId = tenantId;
    }

    /// <summary>
    /// Creates a request-scoping identity from a Teams/activity-facing <see cref="AgenticUser"/>.
    /// </summary>
    /// <param name="user">The agentic user to convert.</param>
    /// <returns>An agentic identity scoped to the supplied user.</returns>
    public static AgenticIdentity FromAgenticUser(AgenticUser user)
    {
        ArgumentNullException.ThrowIfNull(user);

        return new AgenticIdentity(
            AgenticIdentityKind.AgenticUser,
            user.AgenticAppInstanceId!,
            user.AgenticUserId!,
            user.AgenticBlueprintId,
            user.TenantId);
    }

    /// <summary>
    /// Creates a request-scoping identity from an inbound activity recipient account.
    /// </summary>
    /// <param name="recipient">The channel recipient account carrying the typed agentic user fields.</param>
    /// <returns>An agentic identity scoped to the supplied channel recipient.</returns>
    public static AgenticIdentity FromChannelRecipient(ChannelAccount recipient)
    {
        ArgumentNullException.ThrowIfNull(recipient);

        return FromAgenticUser(AgenticUser.FromAccount(recipient)
            ?? throw new ArgumentException("The channel recipient does not contain agentic user information.", nameof(recipient)));
    }

    /// <summary>
    /// Tries to get the concrete Teams/activity-facing agentic user represented by this identity.
    /// </summary>
    /// <param name="agenticUser">The agentic user when this identity is user-scoped; otherwise, null.</param>
    /// <returns><c>true</c> when this identity represents an agentic user; otherwise, <c>false</c>.</returns>
    public bool TryGet([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out AgenticUser? agenticUser)
    {
        if (Kind != AgenticIdentityKind.AgenticUser || string.IsNullOrWhiteSpace(AgenticUserId))
        {
            agenticUser = null;
            return false;
        }

        agenticUser = new AgenticUser
        {
            AgenticAppInstanceId = AgenticAppInstanceId,
            AgenticUserId = AgenticUserId,
            AgenticBlueprintId = AgenticBlueprintId,
            TenantId = TenantId
        };
        return true;
    }

    internal static AgenticIdentity? TryFromAgenticUser(AgenticUser? user)
    {
        if (user is null ||
            string.IsNullOrWhiteSpace(user.AgenticAppInstanceId) ||
            string.IsNullOrWhiteSpace(user.AgenticUserId))
        {
            return null;
        }

        return FromAgenticUser(user);
    }

    internal static AgenticIdentity? TryFromChannelRecipient(ChannelAccount? recipient)
        => TryFromAgenticUser(AgenticUser.FromAccount(recipient));
}
