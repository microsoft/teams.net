// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Globalization;
using System.Text.Json;
using Microsoft.Teams.Core.Schema;
using OpenTelemetry;

namespace Microsoft.Teams.Core.Diagnostics;

/// <summary>
/// Builds OpenTelemetry baggage for Agent365 export from <c>Microsoft.Teams.Core</c> activity types.
/// </summary>
/// <remarks>
/// <para>
/// The Microsoft OpenTelemetry distro's Agent365 exporter stamps every emitted span with the
/// baggage entries set during a turn. This builder populates the cert-required keys
/// (<c>microsoft.tenant.id</c>, <c>gen_ai.conversation.id</c>, <c>microsoft.channel.name</c>, etc.)
/// from a <see cref="CoreActivity"/> without depending on the Apps-layer
/// <c>TeamsConversationAccount</c>. Use the Apps-layer builder
/// (<c>Microsoft.Teams.Apps.Diagnostics.TeamsBaggageBuilder</c>) when you have a
/// <c>Context&lt;TeamsActivity&gt;</c>; it adds the Apps-only keys (<c>user.id</c>, <c>user.email</c>,
/// <c>microsoft.agent.user.email</c>, <c>gen_ai.agent.description</c>).
/// </para>
/// <para>
/// Call <see cref="Build"/> to apply collected pairs to <see cref="Baggage.Current"/>; the returned
/// <see cref="IDisposable"/> restores the previous baggage scope when disposed.
/// </para>
/// <para>
/// See <c>core/docs/Observability-Design.md</c> § "Agent365 baggage and the TurnContext mismatch"
/// for the full cert-attribute mapping.
/// </para>
/// </remarks>
public sealed class CoreBaggageBuilder
{
    private readonly Dictionary<string, string?> _pairs = new(StringComparer.Ordinal);

    /// <summary>Sets the Microsoft Entra tenant id (<c>microsoft.tenant.id</c>). Required for cert.</summary>
    public CoreBaggageBuilder TenantId(string? v) => Set(AgentObservabilityKeys.TenantId, v);

    /// <summary>Sets the conversation id (<c>gen_ai.conversation.id</c>). Required for cert.</summary>
    public CoreBaggageBuilder ConversationId(string? v) => Set(AgentObservabilityKeys.ConversationId, v);

    /// <summary>Sets the conversation item link (<c>microsoft.conversation.item.link</c>). Optional.</summary>
    public CoreBaggageBuilder ConversationItemLink(string? v) => Set(AgentObservabilityKeys.ConversationItemLink, v);

    /// <summary>Sets the channel name (<c>microsoft.channel.name</c>). Required for cert.</summary>
    public CoreBaggageBuilder ChannelName(string? v) => Set(AgentObservabilityKeys.ChannelName, v);

    /// <summary>Sets the channel link (<c>microsoft.channel.link</c>). Optional. Not auto-populated by
    /// <see cref="FromCoreActivity"/> — Teams's flat <c>ChannelId</c> string has no SubChannel concept.</summary>
    public CoreBaggageBuilder ChannelLink(string? v) => Set(AgentObservabilityKeys.ChannelLink, v);

    /// <summary>Sets the agent id (<c>gen_ai.agent.id</c>). Required for cert.</summary>
    public CoreBaggageBuilder AgentId(string? v) => Set(AgentObservabilityKeys.AgentId, v);

    /// <summary>Sets the agent display name (<c>gen_ai.agent.name</c>). Required for cert.</summary>
    public CoreBaggageBuilder AgentName(string? v) => Set(AgentObservabilityKeys.AgentName, v);

    /// <summary>Sets the agentic user id (<c>microsoft.agent.user.id</c>). Required for cert.</summary>
    public CoreBaggageBuilder AgenticUserId(string? v) => Set(AgentObservabilityKeys.AgenticUserId, v);

    /// <summary>Sets the agent blueprint id (<c>microsoft.a365.agent.blueprint.id</c>). Required for cert.</summary>
    public CoreBaggageBuilder AgentBlueprintId(string? v) => Set(AgentObservabilityKeys.AgentBlueprintId, v);

    /// <summary>Sets the human user name (<c>user.name</c>). Optional.</summary>
    public CoreBaggageBuilder UserName(string? v) => Set(AgentObservabilityKeys.UserName, v);

    /// <summary>Sets the operation source (<c>service.name</c>). Required for cert on server spans.</summary>
    public CoreBaggageBuilder OperationSource(string source) => Set(AgentObservabilityKeys.ServiceName, source);

    /// <summary>Sets the InvokeAgent server address and (optional) port. Required for InvokeAgentScope cert.
    /// The port is recorded only when different from the HTTPS default (443).</summary>
    public CoreBaggageBuilder InvokeAgentServer(string? address, int? port = null)
    {
        Set(AgentObservabilityKeys.ServerAddress, address);
        if (port.HasValue && port.Value != 443)
        {
            Set(AgentObservabilityKeys.ServerPort, port.Value.ToString(CultureInfo.InvariantCulture));
        }
        return this;
    }

    /// <summary>Escape hatch for setting any baggage key not exposed by a typed setter
    /// (e.g. <c>user.id</c> / <c>user.email</c> from a non-Apps auth pipeline,
    /// or <c>client.address</c> derived in HTTP middleware).</summary>
    public CoreBaggageBuilder Set(string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(value))
        {
            _pairs[key] = value;
        }
        return this;
    }

    /// <summary>
    /// Populates every baggage key reachable from <paramref name="activity"/>. Falls back to parsing
    /// <c>Properties["channelData"]</c> JSON for <c>tenant.id</c> when <c>Recipient.TenantId</c> is null
    /// (classic Bot Framework Teams traffic carries tenant id in channelData, not on the recipient).
    /// </summary>
    public CoreBaggageBuilder FromCoreActivity(CoreActivity activity)
    {
        ArgumentNullException.ThrowIfNull(activity);

        ConversationId(activity.Conversation?.Id);
        ConversationItemLink(activity.ServiceUrl?.ToString());
        ChannelName(activity.ChannelId);

        UserName(activity.From?.Name);

        ConversationAccount? recipient = activity.Recipient;
        if (recipient is not null)
        {
            AgentId(string.IsNullOrWhiteSpace(recipient.AgenticAppId) ? recipient.Id : recipient.AgenticAppId);
            AgentName(recipient.Name);
            AgenticUserId(recipient.AgenticUserId);
            AgentBlueprintId(recipient.AgenticAppBlueprintId);
            TenantId(recipient.TenantId);
        }

        // Tenant fallback: if Recipient.TenantId is empty, try channelData.tenant.id.
        if (!_pairs.ContainsKey(AgentObservabilityKeys.TenantId))
        {
            string? channelTenantId = TryReadChannelDataTenantId(activity);
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

    private static string? TryReadChannelDataTenantId(CoreActivity activity)
    {
        if (!activity.Properties.TryGetValue("channelData", out object? channelData) || channelData is null)
        {
            return null;
        }

        try
        {
            JsonElement root = channelData switch
            {
                JsonElement je => je,
                _ => JsonSerializer.SerializeToElement(channelData),
            };
            if (root.ValueKind == JsonValueKind.Object &&
                root.TryGetProperty("tenant", out JsonElement tenant) &&
                tenant.ValueKind == JsonValueKind.Object &&
                tenant.TryGetProperty("id", out JsonElement id) &&
                id.ValueKind == JsonValueKind.String)
            {
                return id.GetString();
            }
        }
        catch (JsonException)
        {
            // Best-effort fallback; ignore malformed channelData.
        }

        return null;
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
