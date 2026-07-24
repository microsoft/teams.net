// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Teams.Apps.Utils;

namespace Microsoft.Teams.Apps;

/// <summary>
/// Represents an Agent 365 <c>agentLifecycle</c> event activity.
/// </summary>
public class AgentLifecycleEventActivity : EventActivity
{
    /// <summary>
    /// Gets or sets the lifecycle value payload type. See <see cref="AgentLifecycleEventValueTypes"/> for known values.
    /// </summary>
    [JsonPropertyName("valueType")]
    public AgentLifecycleEventValueType? ValueType { get; internal set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentLifecycleEventActivity"/> class.
    /// </summary>
    [JsonConstructor]
    internal AgentLifecycleEventActivity() : base()
    {
        Name = EventNames.AgentLifecycle;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentLifecycleEventActivity"/> class from an event activity.
    /// </summary>
    /// <param name="activity">The source event activity.</param>
    internal AgentLifecycleEventActivity(EventActivity activity) : base(activity)
    {
        Name ??= EventNames.AgentLifecycle;
        ValueType = Properties.Extract<AgentLifecycleEventValueType>("valueType");
        if (ValueType is null && activity is AgentLifecycleEventActivity lifecycleActivity)
        {
            ValueType = lifecycleActivity.ValueType;
        }
    }
}

/// <summary>
/// Represents an Agent 365 <c>agentLifecycle</c> event activity with a strongly-typed value payload.
/// </summary>
/// <typeparam name="TValue">The lifecycle event value payload type.</typeparam>
public class AgentLifecycleEventActivity<TValue> : AgentLifecycleEventActivity
{
    /// <summary>
    /// Gets or sets the strongly-typed value associated with the lifecycle event activity.
    /// </summary>
    public new TValue? Value
    {
        get => base.Value != null ? JsonSerializer.Deserialize<TValue>(base.Value.ToJsonString()) : default;
        set => base.Value = value != null ? JsonSerializer.SerializeToNode(value) : null;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentLifecycleEventActivity{TValue}"/> class.
    /// </summary>
    internal AgentLifecycleEventActivity() : base()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentLifecycleEventActivity{TValue}"/> class from an event activity.
    /// </summary>
    /// <param name="activity">The source event activity.</param>
    internal AgentLifecycleEventActivity(EventActivity activity) : base(activity)
    {
    }
}

/// <summary>
/// String constants for Agent 365 lifecycle activity <c>valueType</c> values.
/// </summary>
public static class AgentLifecycleEventValueTypes
{
    /// <summary>Agentic user identity created value type.</summary>
    public static AgentLifecycleEventValueType AgenticUserIdentityCreated => AgentLifecycleEventValueType.AgenticUserIdentityCreated;

    /// <summary>Agentic user identity updated value type.</summary>
    public static AgentLifecycleEventValueType AgenticUserIdentityUpdated => AgentLifecycleEventValueType.AgenticUserIdentityUpdated;

    /// <summary>Agentic user manager updated value type.</summary>
    public static AgentLifecycleEventValueType AgenticUserManagerUpdated => AgentLifecycleEventValueType.AgenticUserManagerUpdated;

    /// <summary>Agentic user enabled value type.</summary>
    public static AgentLifecycleEventValueType AgenticUserEnabled => AgentLifecycleEventValueType.AgenticUserEnabled;

    /// <summary>Agentic user disabled value type.</summary>
    public static AgentLifecycleEventValueType AgenticUserDisabled => AgentLifecycleEventValueType.AgenticUserDisabled;

    /// <summary>Agentic user deleted value type.</summary>
    public static AgentLifecycleEventValueType AgenticUserDeleted => AgentLifecycleEventValueType.AgenticUserDeleted;

    /// <summary>Agentic user undeleted value type.</summary>
    public static AgentLifecycleEventValueType AgenticUserUndeleted => AgentLifecycleEventValueType.AgenticUserUndeleted;

    /// <summary>Agentic user workload onboarding updated value type.</summary>
    public static AgentLifecycleEventValueType AgenticUserWorkloadOnboardingUpdated => AgentLifecycleEventValueType.AgenticUserWorkloadOnboardingUpdated;
}

/// <summary>
/// String constants for Agent 365 lifecycle payload <c>eventType</c> values.
/// </summary>
public static class AgentLifecycleEventTypes
{
    /// <summary>Agentic user identity created event type.</summary>
    public static AgentLifecycleEventType AgenticUserIdentityCreated => AgentLifecycleEventType.AgenticUserIdentityCreated;

    /// <summary>Agentic user identity updated event type.</summary>
    public static AgentLifecycleEventType AgenticUserIdentityUpdated => AgentLifecycleEventType.AgenticUserIdentityUpdated;

    /// <summary>Agentic user manager updated event type.</summary>
    public static AgentLifecycleEventType AgenticUserManagerUpdated => AgentLifecycleEventType.AgenticUserManagerUpdated;

    /// <summary>Agentic user enabled event type.</summary>
    public static AgentLifecycleEventType AgenticUserEnabled => AgentLifecycleEventType.AgenticUserEnabled;

    /// <summary>Agentic user disabled event type.</summary>
    public static AgentLifecycleEventType AgenticUserDisabled => AgentLifecycleEventType.AgenticUserDisabled;

    /// <summary>Agentic user deleted event type.</summary>
    public static AgentLifecycleEventType AgenticUserDeleted => AgentLifecycleEventType.AgenticUserDeleted;

    /// <summary>Agentic user undeleted event type.</summary>
    public static AgentLifecycleEventType AgenticUserUndeleted => AgentLifecycleEventType.AgenticUserUndeleted;

    /// <summary>Agentic user workload onboarding updated event type.</summary>
    public static AgentLifecycleEventType AgenticUserWorkloadOnboardingUpdated => AgentLifecycleEventType.AgenticUserWorkloadOnboardingUpdated;
}

/// <summary>
/// String enum for Agent 365 lifecycle value types.
/// </summary>
[JsonConverter(typeof(StringEnumJsonConverter<AgentLifecycleEventValueType>))]
public class AgentLifecycleEventValueType(string value) : StringEnum(value)
{
    /// <summary>Agentic user identity created value type.</summary>
    public static readonly AgentLifecycleEventValueType AgenticUserIdentityCreated = new("AgenticUserIdentityCreated");
    /// <summary>Agentic user identity updated value type.</summary>
    public static readonly AgentLifecycleEventValueType AgenticUserIdentityUpdated = new("AgenticUserIdentityUpdated");
    /// <summary>Agentic user manager updated value type.</summary>
    public static readonly AgentLifecycleEventValueType AgenticUserManagerUpdated = new("AgenticUserManagerUpdated");
    /// <summary>Agentic user enabled value type.</summary>
    public static readonly AgentLifecycleEventValueType AgenticUserEnabled = new("AgenticUserEnabled");
    /// <summary>Agentic user disabled value type.</summary>
    public static readonly AgentLifecycleEventValueType AgenticUserDisabled = new("AgenticUserDisabled");
    /// <summary>Agentic user deleted value type.</summary>
    public static readonly AgentLifecycleEventValueType AgenticUserDeleted = new("AgenticUserDeleted");
    /// <summary>Agentic user undeleted value type.</summary>
    public static readonly AgentLifecycleEventValueType AgenticUserUndeleted = new("AgenticUserUndeleted");
    /// <summary>Agentic user workload onboarding updated value type.</summary>
    public static readonly AgentLifecycleEventValueType AgenticUserWorkloadOnboardingUpdated = new("AgenticUserWorkloadOnboardingUpdated");
}

/// <summary>
/// String enum for Agent 365 lifecycle event types.
/// </summary>
[JsonConverter(typeof(StringEnumJsonConverter<AgentLifecycleEventType>))]
public class AgentLifecycleEventType(string value) : StringEnum(value)
{
    /// <summary>Agentic user identity created event type.</summary>
    public static readonly AgentLifecycleEventType AgenticUserIdentityCreated = new("agenticUserIdentityCreated");
    /// <summary>Agentic user identity updated event type.</summary>
    public static readonly AgentLifecycleEventType AgenticUserIdentityUpdated = new("agenticUserIdentityUpdated");
    /// <summary>Agentic user manager updated event type.</summary>
    public static readonly AgentLifecycleEventType AgenticUserManagerUpdated = new("agenticUserManagerUpdated");
    /// <summary>Agentic user enabled event type.</summary>
    public static readonly AgentLifecycleEventType AgenticUserEnabled = new("agenticUserEnabled");
    /// <summary>Agentic user disabled event type.</summary>
    public static readonly AgentLifecycleEventType AgenticUserDisabled = new("agenticUserDisabled");
    /// <summary>Agentic user deleted event type.</summary>
    public static readonly AgentLifecycleEventType AgenticUserDeleted = new("agenticUserDeleted");
    /// <summary>Agentic user undeleted event type.</summary>
    public static readonly AgentLifecycleEventType AgenticUserUndeleted = new("agenticUserUndeleted");
    /// <summary>Agentic user workload onboarding updated event type.</summary>
    public static readonly AgentLifecycleEventType AgenticUserWorkloadOnboardingUpdated = new("agenticUserWorkloadOnboardingUpdated");
}
