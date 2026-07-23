// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Teams.Apps;

/// <summary>
/// Manager profile carried by the <c>AgenticUserIdentityCreated</c> lifecycle event.
/// </summary>
public class AgentLifecycleManager
{
    /// <summary>
    /// Gets or sets the Entra object ID of the manager.
    /// </summary>
    [JsonPropertyName("userId")]
    public string? UserId { get; set; }

    /// <summary>
    /// Gets or sets the manager's email address.
    /// </summary>
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    /// <summary>
    /// Gets or sets the manager's display name.
    /// </summary>
    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }
}

/// <summary>
/// Manager reference carried by the <c>AgenticUserManagerUpdated</c> lifecycle event.
/// </summary>
public class AgentLifecycleManagerRef
{
    /// <summary>
    /// Gets or sets the Entra object ID of the manager.
    /// </summary>
    [JsonPropertyName("managerId")]
    public string? ManagerId { get; set; }
}

/// <summary>
/// A single property change carried by the <c>AgenticUserIdentityUpdated</c> lifecycle event.
/// </summary>
public class AgentLifecycleUpdatedProperty
{
    /// <summary>
    /// Gets or sets the name of the property that changed.
    /// </summary>
    [JsonPropertyName("propertyName")]
    public required string PropertyName { get; set; }

    /// <summary>
    /// Gets or sets the new value of the property.
    /// </summary>
    [JsonPropertyName("propertyValue")]
    public string? PropertyValue { get; set; }
}

/// <summary>
/// Fields shared by every Agent 365 lifecycle event payload.
/// </summary>
public abstract class AgentLifecycleValueBase
{
    /// <summary>
    /// Gets or sets the tenant that the agentic user belongs to.
    /// </summary>
    [JsonPropertyName("tenantId")]
    public string? TenantId { get; set; }

    /// <summary>
    /// Gets or sets the Agent ID user-shaped identity object ID.
    /// </summary>
    [JsonPropertyName("agenticUserId")]
    public string? AgenticUserId { get; set; }

    /// <summary>
    /// Gets or sets the concrete agent app instance ID.
    /// </summary>
    [JsonPropertyName("agenticAppInstanceId")]
    public string? AgenticAppInstanceId { get; set; }

    /// <summary>
    /// Gets or sets the agent identity blueprint app ID.
    /// </summary>
    [JsonPropertyName("agentIdentityBlueprintId")]
    public string? AgentIdentityBlueprintId { get; set; }

    /// <summary>
    /// Gets or sets the monotonic version of the agentic user state when provided by the service.
    /// </summary>
    [JsonPropertyName("version")]
    public int? Version { get; set; }
}

/// <summary>
/// Payload for the <c>AgenticUserIdentityCreated</c> lifecycle event.
/// </summary>
public class AgenticUserIdentityCreatedValue : AgentLifecycleValueBase
{
    /// <summary>
    /// Gets or sets the lifecycle event type.
    /// </summary>
    [JsonPropertyName("eventType")]
    public string EventType { get; set; } = AgentLifecycleEventTypes.AgenticUserIdentityCreated;

    /// <summary>
    /// Gets or sets the manager assigned to the agentic user at creation.
    /// </summary>
    [JsonPropertyName("manager")]
    public AgentLifecycleManager? Manager { get; set; }

    /// <summary>
    /// Gets or sets when the agentic user identity expires.
    /// </summary>
    [JsonPropertyName("expirationDateTime")]
    public DateTimeOffset? ExpirationDateTime { get; set; }
}

/// <summary>
/// Payload for the <c>AgenticUserIdentityUpdated</c> lifecycle event.
/// </summary>
public class AgenticUserIdentityUpdatedValue : AgentLifecycleValueBase
{
    /// <summary>
    /// Gets or sets the lifecycle event type.
    /// </summary>
    [JsonPropertyName("eventType")]
    public string EventType { get; set; } = AgentLifecycleEventTypes.AgenticUserIdentityUpdated;

    /// <summary>
    /// Gets or sets the property that changed.
    /// </summary>
    [JsonPropertyName("updatedProperty")]
    public required AgentLifecycleUpdatedProperty UpdatedProperty { get; set; }
}

/// <summary>
/// Payload for the <c>AgenticUserManagerUpdated</c> lifecycle event.
/// </summary>
public class AgenticUserManagerUpdatedValue : AgentLifecycleValueBase
{
    /// <summary>
    /// Gets or sets the lifecycle event type.
    /// </summary>
    [JsonPropertyName("eventType")]
    public string EventType { get; set; } = AgentLifecycleEventTypes.AgenticUserManagerUpdated;

    /// <summary>
    /// Gets or sets the new manager reference. The value is absent when the manager was removed.
    /// </summary>
    [JsonPropertyName("manager")]
    public AgentLifecycleManagerRef? Manager { get; set; }
}

/// <summary>
/// Payload for the <c>AgenticUserEnabled</c> lifecycle event.
/// </summary>
public class AgenticUserEnabledValue : AgentLifecycleValueBase
{
    /// <summary>
    /// Gets or sets the lifecycle event type.
    /// </summary>
    [JsonPropertyName("eventType")]
    public string EventType { get; set; } = AgentLifecycleEventTypes.AgenticUserEnabled;
}

/// <summary>
/// Payload for the <c>AgenticUserDisabled</c> lifecycle event.
/// </summary>
public class AgenticUserDisabledValue : AgentLifecycleValueBase
{
    /// <summary>
    /// Gets or sets the lifecycle event type.
    /// </summary>
    [JsonPropertyName("eventType")]
    public string EventType { get; set; } = AgentLifecycleEventTypes.AgenticUserDisabled;
}

/// <summary>
/// Payload for the <c>AgenticUserDeleted</c> lifecycle event.
/// </summary>
public class AgenticUserDeletedValue : AgentLifecycleValueBase
{
    /// <summary>
    /// Gets or sets the lifecycle event type.
    /// </summary>
    [JsonPropertyName("eventType")]
    public string EventType { get; set; } = AgentLifecycleEventTypes.AgenticUserDeleted;

    /// <summary>
    /// Gets or sets the reason the agentic user was deleted.
    /// </summary>
    [JsonPropertyName("deletionReason")]
    public string? DeletionReason { get; set; }
}

/// <summary>
/// Payload for the <c>AgenticUserUndeleted</c> lifecycle event.
/// </summary>
public class AgenticUserUndeletedValue : AgentLifecycleValueBase
{
    /// <summary>
    /// Gets or sets the lifecycle event type.
    /// </summary>
    [JsonPropertyName("eventType")]
    public string EventType { get; set; } = AgentLifecycleEventTypes.AgenticUserUndeleted;
}

/// <summary>
/// Payload for the <c>AgenticUserWorkloadOnboardingUpdated</c> lifecycle event.
/// </summary>
public class AgenticUserWorkloadOnboardingUpdatedValue : AgentLifecycleValueBase
{
    /// <summary>
    /// Gets or sets the lifecycle event type.
    /// </summary>
    [JsonPropertyName("eventType")]
    public string EventType { get; set; } = AgentLifecycleEventTypes.AgenticUserWorkloadOnboardingUpdated;

    /// <summary>
    /// Gets or sets the workload being onboarded.
    /// </summary>
    [JsonPropertyName("workloadName")]
    public string? WorkloadName { get; set; }

    /// <summary>
    /// Gets or sets the onboarding state for the workload.
    /// </summary>
    [JsonPropertyName("workloadOnboardingState")]
    public string? WorkloadOnboardingState { get; set; }
}
