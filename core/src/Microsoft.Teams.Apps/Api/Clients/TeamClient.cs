// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using System.Diagnostics;
using Microsoft.Teams.Apps.Diagnostics;
using Microsoft.Teams.Core.Diagnostics;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Core.Http;
using Microsoft.Teams.Core.Schema;

namespace Microsoft.Teams.Apps.Api.Clients;

/// <summary>
/// Client for retrieving team information and channels.
/// </summary>
public class TeamClient
{
    private readonly BotHttpClient _http;
    private readonly string _serviceUrl;
    private readonly AgenticIdentity? _agenticIdentity;

    internal TeamClient(string serviceUrl, BotHttpClient http, AgenticIdentity? agenticIdentity = null)
    {
        _serviceUrl = serviceUrl.TrimEnd('/');
        _http = http;
        _agenticIdentity = agenticIdentity;
    }

    /// <summary>
    /// Get a team by its ID.
    /// </summary>
    public async Task<Team?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        string url = $"{_serviceUrl}/v3/teams/{Uri.EscapeDataString(id)}";
        return await ExecuteTeamClientAsync(
            AppsTelemetry.ApiOperations.GetTeamById,
            async span => await _http.SendAsync<Team>(HttpMethod.Get, url, body: null, options: CreateRequestOptions(), cancellationToken).ConfigureAwait(false))
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Get the channels (conversations) for a team.
    /// </summary>
    public async Task<List<TeamsChannel>?> GetConversationsAsync(string id, CancellationToken cancellationToken = default)
    {
        string url = $"{_serviceUrl}/v3/teams/{Uri.EscapeDataString(id)}/conversations";
        return await ExecuteTeamClientAsync(
            AppsTelemetry.ApiOperations.GetTeamConversations,
            async span =>
            {
                ConversationListResponse? response = await _http.SendAsync<ConversationListResponse>(HttpMethod.Get, url, body: null, options: CreateRequestOptions(), cancellationToken).ConfigureAwait(false);
                return response?.Conversations;
            }).ConfigureAwait(false);
    }

    private BotRequestContext? AgenticContext => BotRequestContext.FromAgenticIdentity(_agenticIdentity);

    private BotRequestOptions? CreateRequestOptions() =>
        AgenticContext is { } context ? new() { RequestContext = context } : null;

    private async Task<T?> ExecuteTeamClientAsync<T>(string operation, Func<Activity?, Task<T?>> action)
    {
        const string client = "team";
        using Activity? span = AppsTelemetry.Source.StartActivity(AppsTelemetry.Spans.TeamClient, ActivityKind.Client);
        if (span is not null)
        {
            span.SetTag(AppsTelemetry.Tags.Client, client);
            span.SetTag(AppsTelemetry.Tags.Operation, operation);
            span.SetTag(AppsTelemetry.Tags.ServiceUrl, _serviceUrl);
        }

        long start = Stopwatch.GetTimestamp();
        try
        {
            T? result = await action(span).ConfigureAwait(false);
            OutboundTelemetry.RecordCall(client, operation);
            return result;
        }
        catch (Exception ex)
        {
            OutboundTelemetry.RecordError(span, ex, client, operation);
            throw;
        }
        finally
        {
            OutboundTelemetry.RecordDuration(start, client, operation);
        }
    }

    private sealed class ConversationListResponse
    {
        [JsonPropertyName("conversations")]
        public List<TeamsChannel>? Conversations { get; set; }
    }
}
