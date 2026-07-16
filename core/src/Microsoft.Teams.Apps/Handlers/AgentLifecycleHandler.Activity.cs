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
