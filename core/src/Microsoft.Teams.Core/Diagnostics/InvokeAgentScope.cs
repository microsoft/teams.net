// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Text.Json;
using Microsoft.Teams.Core.Schema;

namespace Microsoft.Teams.Core.Diagnostics;

/// <summary>
/// Lightweight <c>InvokeAgentScope</c> that emits an Activity on the <c>Agent365Sdk</c>
/// ActivitySource with the tags the Agent365 exporter requires. This avoids a heavyweight
/// dependency on <c>Microsoft.OpenTelemetry</c> while producing the same span shape.
/// </summary>
/// <remarks>
/// <para>
/// The Agent365 exporter filters spans by <c>gen_ai.operation.name</c> and partitions
/// by <c>microsoft.tenant.id</c> + <c>gen_ai.agent.id</c>. This scope ensures the
/// Teams SDK's turn processing emits a span that passes both gates.
/// </para>
/// <para>
/// Fields reachable only from the Apps-layer <c>TeamsConversationAccount</c>
/// (<c>user.id</c>, <c>user.email</c>, <c>microsoft.agent.user.email</c>,
/// <c>gen_ai.agent.description</c>) are not set here. They reach the exporter via
/// baggage set by the Apps-layer <c>TeamsBaggageBuilder</c>.
/// </para>
/// </remarks>
public sealed class InvokeAgentScope : IDisposable
{
    private const string SourceName = "Agent365Sdk";
    private const string OperationName = "invoke_agent";
    private const string ActivityName = "invoke_agent";
    private const string GenAiOperationNameKey = "gen_ai.operation.name";
    private const string GenAiInputMessagesKey = "gen_ai.input.messages";
    private const string GenAiOutputMessagesKey = "gen_ai.output.messages";
    private const string ErrorTypeKey = "error.type";
    private const string DurationMetricName = "gen_ai.client.operation.duration";
    private const string MessageSchemaVersion = "0.1.0";

    private static readonly ActivitySource s_source = new(SourceName);
    private static readonly Meter s_meter = new(SourceName);
    private static readonly Histogram<double> s_duration = s_meter.CreateHistogram<double>(
        DurationMetricName, "s", "Measures GenAI operation duration.");

    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
    };

    private readonly Activity? _activity;
    private readonly long _startTimestamp;
    private readonly TagList _metricTags;
    private string? _errorType;
    private int _disposed;

    private InvokeAgentScope(Activity? activity, TagList metricTags)
    {
        _activity = activity;
        _metricTags = metricTags;
        _startTimestamp = Stopwatch.GetTimestamp();
        _activity?.Start();
    }

    /// <summary>
    /// Starts an <c>invoke_agent</c> scope populated from a <see cref="CoreActivity"/>.
    /// Returns a disposable scope; the underlying Activity is null (no-op) when no listener
    /// is subscribed to the <c>Agent365Sdk</c> source.
    /// </summary>
    public static InvokeAgentScope Start(CoreActivity activity)
    {
        ArgumentNullException.ThrowIfNull(activity);

        TagList metricTags = new()
        {
            { GenAiOperationNameKey, OperationName },
        };

        Activity? otelActivity = s_source.CreateActivity(ActivityName, ActivityKind.Server);
        if (otelActivity is null)
        {
            return new InvokeAgentScope(null, metricTags);
        }

        // Operation name — required by Agent365 exporter filter.
        otelActivity.SetTag(GenAiOperationNameKey, OperationName);

        // Request-level tags.
        SetTagMaybe(otelActivity, AgentObservabilityKeys.ConversationId, activity.Conversation?.Id);
        SetTagMaybe(otelActivity, AgentObservabilityKeys.ConversationItemLink, activity.ServiceUrl?.ToString());
        SetTagMaybe(otelActivity, AgentObservabilityKeys.ChannelName, activity.ChannelId);

        if (activity.ServiceUrl is not null)
        {
            SetTagMaybe(otelActivity, AgentObservabilityKeys.ServerAddress, activity.ServiceUrl.Host);
            int port = activity.ServiceUrl.Port;
            if (port != 443 && port != -1)
            {
                otelActivity.SetTag(AgentObservabilityKeys.ServerPort, port);
            }
        }

        // Target agent (Recipient).
        ConversationAccount? recipient = activity.Recipient;
        if (recipient is not null)
        {
            string? agentId = string.IsNullOrWhiteSpace(recipient.AgenticAppId) ? recipient.Id : recipient.AgenticAppId;
            SetTagMaybe(otelActivity, AgentObservabilityKeys.AgentId, agentId);
            SetTagMaybe(otelActivity, AgentObservabilityKeys.AgentName, recipient.Name);
            SetTagMaybe(otelActivity, AgentObservabilityKeys.AgenticUserId, recipient.AgenticUserId);
            SetTagMaybe(otelActivity, AgentObservabilityKeys.AgentBlueprintId, recipient.AgenticAppBlueprintId);
            SetTagMaybe(otelActivity, AgentObservabilityKeys.TenantId, recipient.TenantId);
        }

        // Tenant fallback: parse channelData.tenant.id from extension data (same as Core BaggageBuilder).
        if (otelActivity.GetTagItem(AgentObservabilityKeys.TenantId) is null)
        {
            string? channelTenantId = TryReadChannelDataTenantId(activity);
            SetTagMaybe(otelActivity, AgentObservabilityKeys.TenantId, channelTenantId);
        }

        // Caller (human user via From) — only Name is available on Core's ConversationAccount.
        // user.id, user.email are Apps-only (TeamsConversationAccount) and arrive via baggage.
        SetTagMaybe(otelActivity, AgentObservabilityKeys.UserName, activity.From?.Name);

        // Input message from extension-data dictionary.
        string? inputText = activity.Properties.TryGetValue("text", out object? textVal) ? textVal?.ToString() : null;
        if (!string.IsNullOrEmpty(inputText))
        {
            otelActivity.SetTag(GenAiInputMessagesKey, SerializeInputMessages(inputText));
        }

        return new InvokeAgentScope(otelActivity, metricTags);
    }

    /// <summary>
    /// Records output messages on the scope. Call before disposal.
    /// </summary>
    public void RecordOutputMessages(params string[] messages)
    {
        ArgumentNullException.ThrowIfNull(messages);
        if (_activity is null || messages.Length == 0)
        {
            return;
        }

        _activity.SetTag(GenAiOutputMessagesKey, SerializeOutputMessages(messages));
    }

    /// <summary>
    /// Records an error on the scope.
    /// </summary>
    public void RecordError(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        if (_activity is null)
        {
            return;
        }

        _errorType = exception.GetType().FullName;
        _activity.SetStatus(ActivityStatusCode.Error, exception.Message);
        _activity.SetTag(ErrorTypeKey, _errorType);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        double durationSeconds = Stopwatch.GetElapsedTime(_startTimestamp).TotalSeconds;

        TagList finalTags = _metricTags;
        if (_errorType is not null)
        {
            finalTags.Add(ErrorTypeKey, _errorType);
        }

        s_duration.Record(durationSeconds, finalTags);
        _activity?.Dispose();
    }

    private static void SetTagMaybe(Activity activity, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            activity.SetTag(key, value);
        }
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

    private static string SerializeInputMessages(string text)
    {
        MessageEnvelope envelope = new()
        {
            Version = MessageSchemaVersion,
            Messages =
            [
                new MessageEntry
                {
                    Role = "user",
                    Parts = [new TextPart { Content = text }],
                },
            ],
        };
        return JsonSerializer.Serialize(envelope, s_jsonOptions);
    }

    private static string SerializeOutputMessages(string[] texts)
    {
        MessageEntry[] messages = new MessageEntry[texts.Length];
        for (int i = 0; i < texts.Length; i++)
        {
            messages[i] = new MessageEntry
            {
                Role = "assistant",
                Parts = [new TextPart { Content = texts[i] }],
            };
        }

        MessageEnvelope envelope = new() { Version = MessageSchemaVersion, Messages = messages };
        return JsonSerializer.Serialize(envelope, s_jsonOptions);
    }

    // Minimal DTOs matching the Agent365 message schema.
    private sealed class MessageEnvelope
    {
        public string? Version { get; set; }
        public MessageEntry[]? Messages { get; set; }
    }

    private sealed class MessageEntry
    {
        public string? Role { get; set; }
        public TextPart[]? Parts { get; set; }
    }

    private sealed class TextPart
    {
        public string Type { get; } = "text";
        public string? Content { get; set; }
    }
}
