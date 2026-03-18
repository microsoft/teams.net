// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Bot.Apps.Schema;
using Microsoft.Teams.Bot.Core.Schema;

namespace Microsoft.Teams.Bot.Apps.Api;

using CustomHeaders = Dictionary<string, string>;

/// <summary>
/// Provides Teams-specific operations for managing teams and channels.
/// </summary>
public class TeamsOperationsApi
{
    private readonly TeamsApiClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="TeamsOperationsApi"/> class.
    /// </summary>
    /// <param name="teamsApiClient">The Teams API client for team operations.</param>
    internal TeamsOperationsApi(TeamsApiClient teamsApiClient)
    {
        _client = teamsApiClient;
    }

    /// <summary>
    /// Gets details for a team.
    /// </summary>
    /// <param name="teamId">The ID of the team.</param>
    /// <param name="serviceUrl">The service URL for the Teams service.</param>
    /// <param name="agenticIdentity">Optional agentic identity for authentication.</param>
    /// <param name="customHeaders">Optional custom headers to include in the request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the team details.</returns>
    public Task<TeamDetails> GetByIdAsync(
        string teamId,
        Uri serviceUrl,
        AgenticIdentity? agenticIdentity = null,
        CustomHeaders? customHeaders = null,
        CancellationToken cancellationToken = default)
        => _client.FetchTeamDetailsAsync(teamId, serviceUrl, agenticIdentity, customHeaders, cancellationToken);

    /// <summary>
    /// Gets details for a team using activity context, extracting the team ID from channel data.
    /// </summary>
    /// <param name="activity">The activity providing team ID, service URL, and identity context.</param>
    /// <param name="customHeaders">Optional custom headers to include in the request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the team details.</returns>
    public Task<TeamDetails> GetByIdAsync(
        TeamsActivity activity,
        CustomHeaders? customHeaders = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);
        ArgumentNullException.ThrowIfNull(activity.ServiceUrl);
        var teamId = activity.ChannelData?.Team?.Id;
        ArgumentException.ThrowIfNullOrWhiteSpace(teamId, "activity.ChannelData.Team.Id");

        return _client.FetchTeamDetailsAsync(
            teamId,
            activity.ServiceUrl,
            activity.From?.GetAgenticIdentity(),
            customHeaders,
            cancellationToken);
    }

    /// <summary>
    /// Gets the list of channels for a team using activity context.
    /// </summary>
    /// <param name="activity">The activity providing service URL and identity context.</param>
    /// <param name="customHeaders">Optional custom headers to include in the request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the list of channels.</returns>
    public Task<ChannelList> GetChannelsAsync(
        TeamsActivity activity,
        CustomHeaders? customHeaders = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);
        ArgumentNullException.ThrowIfNull(activity.ServiceUrl);
        ArgumentException.ThrowIfNullOrWhiteSpace(activity.ChannelData?.Team?.Id, "activity.ChannelData.Team.Id");

        return _client.FetchChannelListAsync(
            activity.ChannelData.Team.Id,
            activity.ServiceUrl,
            activity.From?.GetAgenticIdentity(),
            customHeaders,
            cancellationToken);
    }
}
