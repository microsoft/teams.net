// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Teams.Apps.Handlers;

/// <summary>
/// Represents an Agent 365 <c>agentLifecycle</c> event activity.
/// </summary>
public class AgentLifecycleEventActivity : EventActivity
{
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
    }

    /// <summary>
    /// Creates the most-specific Agent 365 lifecycle activity type for the supplied event activity.
    /// </summary>
    /// <param name="activity">The source event activity.</param>
    /// <returns>A typed lifecycle event activity when the <c>valueType</c> is known; otherwise a base lifecycle activity.</returns>
    internal static AgentLifecycleEventActivity FromEventActivity(EventActivity activity)
    {
        ArgumentNullException.ThrowIfNull(activity);

        return activity.ValueType switch
        {
            AgentLifecycleEventValueTypes.AgenticUserIdentityCreated => new AgenticUserIdentityCreatedActivity(activity),
            AgentLifecycleEventValueTypes.AgenticUserIdentityUpdated => new AgenticUserIdentityUpdatedActivity(activity),
            AgentLifecycleEventValueTypes.AgenticUserManagerUpdated => new AgenticUserManagerUpdatedActivity(activity),
            AgentLifecycleEventValueTypes.AgenticUserEnabled => new AgenticUserEnabledActivity(activity),
            AgentLifecycleEventValueTypes.AgenticUserDisabled => new AgenticUserDisabledActivity(activity),
            AgentLifecycleEventValueTypes.AgenticUserDeleted => new AgenticUserDeletedActivity(activity),
            AgentLifecycleEventValueTypes.AgenticUserUndeleted => new AgenticUserUndeletedActivity(activity),
            AgentLifecycleEventValueTypes.AgenticUserWorkloadOnboardingUpdated => new AgenticUserWorkloadOnboardingUpdatedActivity(activity),
            _ => new AgentLifecycleEventActivity(activity),
        };
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
    [JsonPropertyName("value")]
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
/// Fired when an agentic user identity is created.
/// </summary>
public class AgenticUserIdentityCreatedActivity : AgentLifecycleEventActivity<AgenticUserIdentityCreatedValue>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AgenticUserIdentityCreatedActivity"/> class.
    /// </summary>
    [JsonConstructor]
    internal AgenticUserIdentityCreatedActivity() : base()
    {
        ValueType = AgentLifecycleEventValueTypes.AgenticUserIdentityCreated;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AgenticUserIdentityCreatedActivity"/> class from an event activity.
    /// </summary>
    /// <param name="activity">The source event activity.</param>
    internal AgenticUserIdentityCreatedActivity(EventActivity activity) : base(activity)
    {
        ValueType ??= AgentLifecycleEventValueTypes.AgenticUserIdentityCreated;
    }
}

/// <summary>
/// Fired when an agentic user identity property changes.
/// </summary>
public class AgenticUserIdentityUpdatedActivity : AgentLifecycleEventActivity<AgenticUserIdentityUpdatedValue>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AgenticUserIdentityUpdatedActivity"/> class.
    /// </summary>
    [JsonConstructor]
    internal AgenticUserIdentityUpdatedActivity() : base()
    {
        ValueType = AgentLifecycleEventValueTypes.AgenticUserIdentityUpdated;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AgenticUserIdentityUpdatedActivity"/> class from an event activity.
    /// </summary>
    /// <param name="activity">The source event activity.</param>
    internal AgenticUserIdentityUpdatedActivity(EventActivity activity) : base(activity)
    {
        ValueType ??= AgentLifecycleEventValueTypes.AgenticUserIdentityUpdated;
    }
}

/// <summary>
/// Fired when an agentic user's manager changes.
/// </summary>
public class AgenticUserManagerUpdatedActivity : AgentLifecycleEventActivity<AgenticUserManagerUpdatedValue>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AgenticUserManagerUpdatedActivity"/> class.
    /// </summary>
    [JsonConstructor]
    internal AgenticUserManagerUpdatedActivity() : base()
    {
        ValueType = AgentLifecycleEventValueTypes.AgenticUserManagerUpdated;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AgenticUserManagerUpdatedActivity"/> class from an event activity.
    /// </summary>
    /// <param name="activity">The source event activity.</param>
    internal AgenticUserManagerUpdatedActivity(EventActivity activity) : base(activity)
    {
        ValueType ??= AgentLifecycleEventValueTypes.AgenticUserManagerUpdated;
    }
}

/// <summary>
/// Fired when an agentic user is enabled.
/// </summary>
public class AgenticUserEnabledActivity : AgentLifecycleEventActivity<AgenticUserEnabledValue>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AgenticUserEnabledActivity"/> class.
    /// </summary>
    [JsonConstructor]
    internal AgenticUserEnabledActivity() : base()
    {
        ValueType = AgentLifecycleEventValueTypes.AgenticUserEnabled;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AgenticUserEnabledActivity"/> class from an event activity.
    /// </summary>
    /// <param name="activity">The source event activity.</param>
    internal AgenticUserEnabledActivity(EventActivity activity) : base(activity)
    {
        ValueType ??= AgentLifecycleEventValueTypes.AgenticUserEnabled;
    }
}

/// <summary>
/// Fired when an agentic user is disabled.
/// </summary>
public class AgenticUserDisabledActivity : AgentLifecycleEventActivity<AgenticUserDisabledValue>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AgenticUserDisabledActivity"/> class.
    /// </summary>
    [JsonConstructor]
    internal AgenticUserDisabledActivity() : base()
    {
        ValueType = AgentLifecycleEventValueTypes.AgenticUserDisabled;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AgenticUserDisabledActivity"/> class from an event activity.
    /// </summary>
    /// <param name="activity">The source event activity.</param>
    internal AgenticUserDisabledActivity(EventActivity activity) : base(activity)
    {
        ValueType ??= AgentLifecycleEventValueTypes.AgenticUserDisabled;
    }
}

/// <summary>
/// Fired when an agentic user is deleted.
/// </summary>
public class AgenticUserDeletedActivity : AgentLifecycleEventActivity<AgenticUserDeletedValue>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AgenticUserDeletedActivity"/> class.
    /// </summary>
    [JsonConstructor]
    internal AgenticUserDeletedActivity() : base()
    {
        ValueType = AgentLifecycleEventValueTypes.AgenticUserDeleted;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AgenticUserDeletedActivity"/> class from an event activity.
    /// </summary>
    /// <param name="activity">The source event activity.</param>
    internal AgenticUserDeletedActivity(EventActivity activity) : base(activity)
    {
        ValueType ??= AgentLifecycleEventValueTypes.AgenticUserDeleted;
    }
}

/// <summary>
/// Fired when a previously deleted agentic user is restored.
/// </summary>
public class AgenticUserUndeletedActivity : AgentLifecycleEventActivity<AgenticUserUndeletedValue>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AgenticUserUndeletedActivity"/> class.
    /// </summary>
    [JsonConstructor]
    internal AgenticUserUndeletedActivity() : base()
    {
        ValueType = AgentLifecycleEventValueTypes.AgenticUserUndeleted;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AgenticUserUndeletedActivity"/> class from an event activity.
    /// </summary>
    /// <param name="activity">The source event activity.</param>
    internal AgenticUserUndeletedActivity(EventActivity activity) : base(activity)
    {
        ValueType ??= AgentLifecycleEventValueTypes.AgenticUserUndeleted;
    }
}

/// <summary>
/// Fired when a workload onboarding state changes for an agentic user.
/// </summary>
public class AgenticUserWorkloadOnboardingUpdatedActivity : AgentLifecycleEventActivity<AgenticUserWorkloadOnboardingUpdatedValue>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AgenticUserWorkloadOnboardingUpdatedActivity"/> class.
    /// </summary>
    [JsonConstructor]
    internal AgenticUserWorkloadOnboardingUpdatedActivity() : base()
    {
        ValueType = AgentLifecycleEventValueTypes.AgenticUserWorkloadOnboardingUpdated;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AgenticUserWorkloadOnboardingUpdatedActivity"/> class from an event activity.
    /// </summary>
    /// <param name="activity">The source event activity.</param>
    internal AgenticUserWorkloadOnboardingUpdatedActivity(EventActivity activity) : base(activity)
    {
        ValueType ??= AgentLifecycleEventValueTypes.AgenticUserWorkloadOnboardingUpdated;
    }
}

/// <summary>
/// String constants for Agent 365 lifecycle activity <c>valueType</c> values.
/// </summary>
public static class AgentLifecycleEventValueTypes
{
    /// <summary>Agentic user identity created value type.</summary>
    public const string AgenticUserIdentityCreated = "AgenticUserIdentityCreated";

    /// <summary>Agentic user identity updated value type.</summary>
    public const string AgenticUserIdentityUpdated = "AgenticUserIdentityUpdated";

    /// <summary>Agentic user manager updated value type.</summary>
    public const string AgenticUserManagerUpdated = "AgenticUserManagerUpdated";

    /// <summary>Agentic user enabled value type.</summary>
    public const string AgenticUserEnabled = "AgenticUserEnabled";

    /// <summary>Agentic user disabled value type.</summary>
    public const string AgenticUserDisabled = "AgenticUserDisabled";

    /// <summary>Agentic user deleted value type.</summary>
    public const string AgenticUserDeleted = "AgenticUserDeleted";

    /// <summary>Agentic user undeleted value type.</summary>
    public const string AgenticUserUndeleted = "AgenticUserUndeleted";

    /// <summary>Agentic user workload onboarding updated value type.</summary>
    public const string AgenticUserWorkloadOnboardingUpdated = "AgenticUserWorkloadOnboardingUpdated";
}

/// <summary>
/// String constants for Agent 365 lifecycle payload <c>eventType</c> values.
/// </summary>
public static class AgentLifecycleEventTypes
{
    /// <summary>Agentic user identity created event type.</summary>
    public const string AgenticUserIdentityCreated = "agenticUserIdentityCreated";

    /// <summary>Agentic user identity updated event type.</summary>
    public const string AgenticUserIdentityUpdated = "agenticUserIdentityUpdated";

    /// <summary>Agentic user manager updated event type.</summary>
    public const string AgenticUserManagerUpdated = "agenticUserManagerUpdated";

    /// <summary>Agentic user enabled event type.</summary>
    public const string AgenticUserEnabled = "agenticUserEnabled";

    /// <summary>Agentic user disabled event type.</summary>
    public const string AgenticUserDisabled = "agenticUserDisabled";

    /// <summary>Agentic user deleted event type.</summary>
    public const string AgenticUserDeleted = "agenticUserDeleted";

    /// <summary>Agentic user undeleted event type.</summary>
    public const string AgenticUserUndeleted = "agenticUserUndeleted";

    /// <summary>Agentic user workload onboarding updated event type.</summary>
    public const string AgenticUserWorkloadOnboardingUpdated = "agenticUserWorkloadOnboardingUpdated";
}
