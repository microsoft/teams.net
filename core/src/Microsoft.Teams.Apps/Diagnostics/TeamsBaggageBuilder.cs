// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Globalization;
using Microsoft.Teams.Apps.Schema;
using OpenTelemetry;

namespace Microsoft.Teams.Apps.Diagnostics;

/// <summary>
/// Builds OpenTelemetry baggage for Agent365 export from <c>Microsoft.Teams.Apps</c> turn types.
/// </summary>
/// <remarks>
/// <para>
/// Populates the cert-required keys (<c>microsoft.tenant.id</c>, <c>gen_ai.conversation.id</c>,
/// <c>microsoft.channel.name</c>, etc.) plus the Apps-only keys backed by
/// <see cref="TeamsChannelAccount"/> (<c>user.id</c>, <c>user.email</c>,
/// <c>microsoft.agent.user.email</c>, <c>gen_ai.agent.description</c>).
/// </para>
/// </remarks>
public sealed class TeamsBaggageBuilder
{
    private readonly Dictionary<string, string?> _pairs = new(StringComparer.Ordinal);

    /// <summary>Sets the Microsoft Entra tenant id (<c>microsoft.tenant.id</c>). Required for cert.</summary>
    public TeamsBaggageBuilder TenantId(string? v) => Set(AgentObservabilityKeys.TenantId, v);

    /// <summary>Sets the conversation id (<c>gen_ai.conversation.id</c>). Required for cert.</summary>
    public TeamsBaggageBuilder ConversationId(string? v) => Set(AgentObservabilityKeys.ConversationId, v);

    /// <summary>Sets the conversation item link (<c>microsoft.conversation.item.link</c>). Optional.</summary>
    public TeamsBaggageBuilder ConversationItemLink(string? v) => Set(AgentObservabilityKeys.ConversationItemLink, v);

    /// <summary>Sets the channel name (<c>microsoft.channel.name</c>). Required for cert.</summary>
    public TeamsBaggageBuilder ChannelName(string? v) => Set(AgentObservabilityKeys.ChannelName, v);

    /// <summary>Sets the channel link (<c>microsoft.channel.link</c>). Optional. Not auto-populated by
    /// <see cref="FromTeamsContext"/> — Teams's flat <c>ChannelId</c> string has no SubChannel concept.</summary>
    public TeamsBaggageBuilder ChannelLink(string? v) => Set(AgentObservabilityKeys.ChannelLink, v);

    /// <summary>Sets the agent id (<c>gen_ai.agent.id</c>). Required for cert.</summary>
    public TeamsBaggageBuilder AgentId(string? v) => Set(AgentObservabilityKeys.AgentId, v);

    /// <summary>Sets the agent display name (<c>gen_ai.agent.name</c>). Required for cert.</summary>
    public TeamsBaggageBuilder AgentName(string? v) => Set(AgentObservabilityKeys.AgentName, v);

    /// <summary>Sets the agentic user id (<c>microsoft.agent.user.id</c>). Required for cert.</summary>
    public TeamsBaggageBuilder AgenticUserId(string? v) => Set(AgentObservabilityKeys.AgenticUserId, v);

    /// <summary>Sets the agent blueprint id (<c>microsoft.a365.agent.blueprint.id</c>). Required for cert.</summary>
    public TeamsBaggageBuilder AgentBlueprintId(string? v) => Set(AgentObservabilityKeys.AgentBlueprintId, v);

    /// <summary>Sets the human user name (<c>user.name</c>). Optional.</summary>
    public TeamsBaggageBuilder UserName(string? v) => Set(AgentObservabilityKeys.UserName, v);

    /// <summary>Sets the operation source (<c>service.name</c>). Required for cert on server spans.</summary>
    public TeamsBaggageBuilder OperationSource(string source) => Set(AgentObservabilityKeys.ServiceName, source);

    /// <summary>Sets the InvokeAgent server address and (optional) port. Required for InvokeAgentScope cert.
    /// The port is recorded only when different from the HTTPS default (443).</summary>
    public TeamsBaggageBuilder InvokeAgentServer(string? address, int? port = null)
    {
        Set(AgentObservabilityKeys.ServerAddress, address);
        if (port.HasValue && port.Value != 443)
        {
            Set(AgentObservabilityKeys.ServerPort, port.Value.ToString(CultureInfo.InvariantCulture));
        }
        return this;
    }

    /// <summary>Sets the human user id (<c>user.id</c>). Required for cert. Apps-only — backed by
    /// <see cref="TeamsChannelAccount.AadObjectId"/>.</summary>
    public TeamsBaggageBuilder UserId(string? v) => Set(AgentObservabilityKeys.UserId, v);

    /// <summary>Sets the human user email (<c>user.email</c>). Required for cert. Apps-only.</summary>
    public TeamsBaggageBuilder UserEmail(string? v) => Set(AgentObservabilityKeys.UserEmail, v);

    /// <summary>Sets the agent description (<c>gen_ai.agent.description</c>). Optional. Apps-only —
    /// backed by <see cref="TeamsChannelAccount.UserRole"/>.</summary>
    public TeamsBaggageBuilder AgentDescription(string? v) => Set(AgentObservabilityKeys.AgentDescription, v);

    /// <summary>Sets the agentic user email (<c>microsoft.agent.user.email</c>). Required for cert. Apps-only.</summary>
    public TeamsBaggageBuilder AgenticUserEmail(string? v) => Set(AgentObservabilityKeys.AgenticUserEmail, v);

    /// <summary>Escape hatch for setting any baggage key not exposed by a typed setter
    /// (e.g. <c>client.address</c> derived in HTTP middleware).</summary>
    public TeamsBaggageBuilder Set(string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(value))
        {
            _pairs[key] = value;
        }
        return this;
    }

    /// <summary>
    /// Populates every baggage key reachable from <c>ctx.Activity</c> — including the Apps-only keys
    /// backed by <see cref="TeamsChannelAccount"/>. Tenant fallback uses the typed
    /// <see cref="TeamsChannelData"/> when <see cref="Core.Schema.ChannelAccount.TenantId"/> is null.
    /// </summary>
    public TeamsBaggageBuilder FromTeamsContext<TActivity>(Context<TActivity> ctx) where TActivity : TeamsActivity
    {
        ArgumentNullException.ThrowIfNull(ctx);
        TActivity activity = ctx.Activity;

        ConversationId(activity.Conversation?.Id);
        ConversationItemLink(activity.ServiceUrl?.ToString());
        ChannelName(activity.ChannelId);

        UserName(activity.From?.Name);
if (activity.From is TeamsChannelAccount fromAccount)
{
    UserId(fromAccount.AadObjectId);
    UserEmail(fromAccount.Email);
}

        TeamsChannelAccount? recipient = activity.Recipient;
        if (recipient is not null)
        {
            AgentId(string.IsNullOrWhiteSpace(recipient.AgenticAppId) ? recipient.Id : recipient.AgenticAppId);
            AgentName(recipient.Name);
            AgenticUserId(recipient.AgenticUserId);
            AgentBlueprintId(recipient.AgenticAppBlueprintId);
            TenantId(recipient.TenantId);
            AgenticUserEmail(recipient.Email);
            AgentDescription(recipient.UserRole);
        }

        // Tenant fallback: typed channelData on TeamsActivity (no JSON parse needed).
        if (!_pairs.ContainsKey(AgentObservabilityKeys.TenantId))
        {
            string? channelTenantId = activity.ChannelData?.Tenant?.Id;
            if (!string.IsNullOrWhiteSpace(channelTenantId))
            {
                TenantId(channelTenantId);
            }
        }

        return this;
    }

    /// <summary>
    /// Applies the collected pairs to <see cref="Baggage.Current"/> and returns an
    /// <see cref="IDisposable"/> that restores the previous baggage when disposed.
    /// </summary>
    public IDisposable Build()
    {
        Baggage previous = Baggage.Current;
        foreach (KeyValuePair<string, string?> kvp in _pairs)
        {
            Baggage.Current = Baggage.Current.SetBaggage(kvp.Key, kvp.Value);
        }
        return new RestoreScope(previous);
    }

    private sealed class RestoreScope(Baggage previous) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            Baggage.Current = previous;
            _disposed = true;
        }
    }
}
