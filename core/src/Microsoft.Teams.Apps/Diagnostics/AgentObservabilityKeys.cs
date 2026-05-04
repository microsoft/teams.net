// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Teams.Apps.Diagnostics;

/// <summary>
/// Agent365 observability baggage and attribute keys, duplicated from
/// <c>Microsoft.Agents.A365.Observability.Runtime.Tracing.Scopes.OpenTelemetryConstants</c>.
/// Same wire values as <c>Microsoft.Teams.Core.Diagnostics.AgentObservabilityKeys</c>; duplicated
/// per layer to keep Apps independent of Core's internals (see Layering constraints in
/// <c>core/docs/Observability-Design.md</c>).
/// </summary>
internal static class AgentObservabilityKeys
{
    public const string TenantId            = "microsoft.tenant.id";
    public const string ConversationId      = "gen_ai.conversation.id";
    public const string ConversationItemLink = "microsoft.conversation.item.link";
    public const string ChannelName         = "microsoft.channel.name";
    public const string ChannelLink         = "microsoft.channel.link";

    public const string UserId              = "user.id";
    public const string UserEmail           = "user.email";
    public const string UserName            = "user.name";
    public const string ClientAddress       = "client.address";

    public const string AgentId             = "gen_ai.agent.id";
    public const string AgentName           = "gen_ai.agent.name";
    public const string AgentDescription    = "gen_ai.agent.description";
    public const string AgentVersion        = "gen_ai.agent.version";
    public const string AgenticUserId       = "microsoft.agent.user.id";
    public const string AgenticUserEmail    = "microsoft.agent.user.email";
    public const string AgentBlueprintId    = "microsoft.a365.agent.blueprint.id";
    public const string AgentPlatformId     = "microsoft.a365.agent.platform.id";

    public const string CallerAgentName       = "microsoft.a365.caller.agent.name";
    public const string CallerAgentId         = "microsoft.a365.caller.agent.id";
    public const string CallerAgentBlueprintId = "microsoft.a365.caller.agent.blueprint.id";
    public const string CallerAgentUserId     = "microsoft.a365.caller.agent.user.id";
    public const string CallerAgentUserEmail  = "microsoft.a365.caller.agent.user.email";
    public const string CallerAgentPlatformId = "microsoft.a365.caller.agent.platform.id";
    public const string CallerAgentVersion    = "microsoft.a365.caller.agent.version";

    public const string SessionId           = "microsoft.session.id";
    public const string SessionDescription  = "microsoft.session.description";

    public const string ServiceName         = "service.name";
    public const string ServerAddress       = "server.address";
    public const string ServerPort          = "server.port";
}
