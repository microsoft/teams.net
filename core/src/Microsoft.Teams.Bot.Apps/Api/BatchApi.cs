// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Bot.Apps.Schema;
using Microsoft.Teams.Bot.Core.Schema;

namespace Microsoft.Teams.Bot.Apps.Api;

using CustomHeaders = Dictionary<string, string>;

/// <summary>
/// Provides batch messaging operations for sending messages to multiple recipients.
/// </summary>
public class BatchApi
{
    private readonly TeamsApiClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="BatchApi"/> class.
    /// </summary>
    /// <param name="teamsApiClient">The Teams API client for batch operations.</param>
    internal BatchApi(TeamsApiClient teamsApiClient)
    {
        _client = teamsApiClient;
    }

    /// <summary>
    /// Sends a message to a list of Teams users.
    /// </summary>
    /// <param name="activity">The activity to send.</param>
    /// <param name="teamsMembers">The list of team members to send the message to.</param>
    /// <param name="tenantId">The ID of the tenant.</param>
    /// <param name="serviceUrl">The service URL for the Teams service.</param>
    /// <param name="agenticIdentity">Optional agentic identity for authentication.</param>
    /// <param name="customHeaders">Optional custom headers to include in the request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the operation ID.</returns>
    public Task<string> SendToUsersAsync(
        CoreActivity activity,
        IList<TeamMember> teamsMembers,
        string tenantId,
        Uri serviceUrl,
        AgenticIdentity? agenticIdentity = null,
        CustomHeaders? customHeaders = null,
        CancellationToken cancellationToken = default)
        => _client.SendMessageToListOfUsersAsync(activity, teamsMembers, tenantId, serviceUrl, agenticIdentity, customHeaders, cancellationToken);

    /// <summary>
    /// Sends a message to a list of Teams users using activity context.
    /// </summary>
    /// <param name="activity">The activity to send.</param>
    /// <param name="teamsMembers">The list of team members to send the message to.</param>
    /// <param name="contextActivity">The activity providing service URL, tenant ID, and identity context.</param>
    /// <param name="customHeaders">Optional custom headers to include in the request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the operation ID.</returns>
    public Task<string> SendToUsersAsync(
        CoreActivity activity,
        IList<TeamMember> teamsMembers,
        TeamsActivity contextActivity,
        CustomHeaders? customHeaders = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(contextActivity);
        return _client.SendMessageToListOfUsersAsync(
            activity,
            teamsMembers,
            contextActivity.ChannelData?.Tenant?.Id ?? throw new InvalidOperationException("Tenant ID not available in activity"),
            contextActivity.ServiceUrl!,
            contextActivity.From.GetAgenticIdentity(),
            customHeaders,
            cancellationToken);
    }

    /// <summary>
    /// Sends a message to all users in a tenant.
    /// </summary>
    /// <param name="activity">The activity to send.</param>
    /// <param name="tenantId">The ID of the tenant.</param>
    /// <param name="serviceUrl">The service URL for the Teams service.</param>
    /// <param name="agenticIdentity">Optional agentic identity for authentication.</param>
    /// <param name="customHeaders">Optional custom headers to include in the request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the operation ID.</returns>
    public Task<string> SendToTenantAsync(
        CoreActivity activity,
        string tenantId,
        Uri serviceUrl,
        AgenticIdentity? agenticIdentity = null,
        CustomHeaders? customHeaders = null,
        CancellationToken cancellationToken = default)
        => _client.SendMessageToAllUsersInTenantAsync(activity, tenantId, serviceUrl, agenticIdentity, customHeaders, cancellationToken);

    /// <summary>
    /// Sends a message to all users in a tenant using activity context.
    /// </summary>
    /// <param name="activity">The activity to send.</param>
    /// <param name="contextActivity">The activity providing service URL, tenant ID, and identity context.</param>
    /// <param name="customHeaders">Optional custom headers to include in the request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the operation ID.</returns>
    public Task<string> SendToTenantAsync(
        CoreActivity activity,
        TeamsActivity contextActivity,
        CustomHeaders? customHeaders = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(contextActivity);
        return _client.SendMessageToAllUsersInTenantAsync(
            activity,
            contextActivity.ChannelData?.Tenant?.Id ?? throw new InvalidOperationException("Tenant ID not available in activity"),
            contextActivity.ServiceUrl!,
            contextActivity.From.GetAgenticIdentity(),
            customHeaders,
            cancellationToken);
    }

    /// <summary>
    /// Sends a message to all users in a team.
    /// </summary>
    /// <param name="activity">The activity to send.</param>
    /// <param name="teamId">The ID of the team.</param>
    /// <param name="tenantId">The ID of the tenant.</param>
    /// <param name="serviceUrl">The service URL for the Teams service.</param>
    /// <param name="agenticIdentity">Optional agentic identity for authentication.</param>
    /// <param name="customHeaders">Optional custom headers to include in the request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the operation ID.</returns>
    public Task<string> SendToTeamAsync(
        CoreActivity activity,
        string teamId,
        string tenantId,
        Uri serviceUrl,
        AgenticIdentity? agenticIdentity = null,
        CustomHeaders? customHeaders = null,
        CancellationToken cancellationToken = default)
        => _client.SendMessageToAllUsersInTeamAsync(activity, teamId, tenantId, serviceUrl, agenticIdentity, customHeaders, cancellationToken);

    /// <summary>
    /// Sends a message to all users in a team using activity context.
    /// </summary>
    /// <param name="activity">The activity to send.</param>
    /// <param name="teamId">The ID of the team.</param>
    /// <param name="contextActivity">The activity providing service URL, tenant ID, and identity context.</param>
    /// <param name="customHeaders">Optional custom headers to include in the request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the operation ID.</returns>
    public Task<string> SendToTeamAsync(
        CoreActivity activity,
        string teamId,
        TeamsActivity contextActivity,
        CustomHeaders? customHeaders = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(contextActivity);
        return _client.SendMessageToAllUsersInTeamAsync(
            activity,
            teamId,
            contextActivity.ChannelData?.Tenant?.Id ?? throw new InvalidOperationException("Tenant ID not available in activity"),
            contextActivity.ServiceUrl!,
            contextActivity.From.GetAgenticIdentity(),
            customHeaders,
            cancellationToken);
    }

    /// <summary>
    /// Sends a message to a list of Teams channels.
    /// </summary>
    /// <param name="activity">The activity to send.</param>
    /// <param name="channelMembers">The list of channels to send the message to.</param>
    /// <param name="tenantId">The ID of the tenant.</param>
    /// <param name="serviceUrl">The service URL for the Teams service.</param>
    /// <param name="agenticIdentity">Optional agentic identity for authentication.</param>
    /// <param name="customHeaders">Optional custom headers to include in the request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the operation ID.</returns>
    public Task<string> SendToChannelsAsync(
        CoreActivity activity,
        IList<TeamMember> channelMembers,
        string tenantId,
        Uri serviceUrl,
        AgenticIdentity? agenticIdentity = null,
        CustomHeaders? customHeaders = null,
        CancellationToken cancellationToken = default)
        => _client.SendMessageToListOfChannelsAsync(activity, channelMembers, tenantId, serviceUrl, agenticIdentity, customHeaders, cancellationToken);

    /// <summary>
    /// Sends a message to a list of Teams channels using activity context.
    /// </summary>
    /// <param name="activity">The activity to send.</param>
    /// <param name="channelMembers">The list of channels to send the message to.</param>
    /// <param name="contextActivity">The activity providing service URL, tenant ID, and identity context.</param>
    /// <param name="customHeaders">Optional custom headers to include in the request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the operation ID.</returns>
    public Task<string> SendToChannelsAsync(
        CoreActivity activity,
        IList<TeamMember> channelMembers,
        TeamsActivity contextActivity,
        CustomHeaders? customHeaders = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(contextActivity);
        return _client.SendMessageToListOfChannelsAsync(
            activity,
            channelMembers,
            contextActivity.ChannelData?.Tenant?.Id ?? throw new InvalidOperationException("Tenant ID not available in activity"),
            contextActivity.ServiceUrl!,
            contextActivity.From.GetAgenticIdentity(),
            customHeaders,
            cancellationToken);
    }

    /// <summary>
    /// Gets the state of a batch operation.
    /// </summary>
    /// <param name="operationId">The ID of the operation.</param>
    /// <param name="serviceUrl">The service URL for the Teams service.</param>
    /// <param name="agenticIdentity">Optional agentic identity for authentication.</param>
    /// <param name="customHeaders">Optional custom headers to include in the request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the operation state.</returns>
    public Task<BatchOperationState> GetStateAsync(
        string operationId,
        Uri serviceUrl,
        AgenticIdentity? agenticIdentity = null,
        CustomHeaders? customHeaders = null,
        CancellationToken cancellationToken = default)
        => _client.GetOperationStateAsync(operationId, serviceUrl, agenticIdentity, customHeaders, cancellationToken);

    /// <summary>
    /// Gets the state of a batch operation using activity context.
    /// </summary>
    /// <param name="operationId">The ID of the operation.</param>
    /// <param name="activity">The activity providing service URL and identity context.</param>
    /// <param name="customHeaders">Optional custom headers to include in the request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the operation state.</returns>
    public Task<BatchOperationState> GetStateAsync(
        string operationId,
        TeamsActivity activity,
        CustomHeaders? customHeaders = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);
        return _client.GetOperationStateAsync(
            operationId,
            activity.ServiceUrl!,
            activity.From.GetAgenticIdentity(),
            customHeaders,
            cancellationToken);
    }

    /// <summary>
    /// Gets the failed entries of a batch operation.
    /// </summary>
    /// <param name="operationId">The ID of the operation.</param>
    /// <param name="serviceUrl">The service URL for the Teams service.</param>
    /// <param name="continuationToken">Optional continuation token for pagination.</param>
    /// <param name="agenticIdentity">Optional agentic identity for authentication.</param>
    /// <param name="customHeaders">Optional custom headers to include in the request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the failed entries.</returns>
    public Task<BatchFailedEntriesResponse> GetFailedEntriesAsync(
        string operationId,
        Uri serviceUrl,
        string? continuationToken = null,
        AgenticIdentity? agenticIdentity = null,
        CustomHeaders? customHeaders = null,
        CancellationToken cancellationToken = default)
        => _client.GetPagedFailedEntriesAsync(operationId, serviceUrl, continuationToken, agenticIdentity, customHeaders, cancellationToken);

    /// <summary>
    /// Gets the failed entries of a batch operation using activity context.
    /// </summary>
    /// <param name="operationId">The ID of the operation.</param>
    /// <param name="activity">The activity providing service URL and identity context.</param>
    /// <param name="continuationToken">Optional continuation token for pagination.</param>
    /// <param name="customHeaders">Optional custom headers to include in the request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the failed entries.</returns>
    public Task<BatchFailedEntriesResponse> GetFailedEntriesAsync(
        string operationId,
        TeamsActivity activity,
        string? continuationToken = null,
        CustomHeaders? customHeaders = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);
        return _client.GetPagedFailedEntriesAsync(
            operationId,
            activity.ServiceUrl!,
            continuationToken,
            activity.From.GetAgenticIdentity(),
            customHeaders,
            cancellationToken);
    }

    /// <summary>
    /// Cancels a batch operation.
    /// </summary>
    /// <param name="operationId">The ID of the operation to cancel.</param>
    /// <param name="serviceUrl">The service URL for the Teams service.</param>
    /// <param name="agenticIdentity">Optional agentic identity for authentication.</param>
    /// <param name="customHeaders">Optional custom headers to include in the request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task CancelAsync(
        string operationId,
        Uri serviceUrl,
        AgenticIdentity? agenticIdentity = null,
        CustomHeaders? customHeaders = null,
        CancellationToken cancellationToken = default)
        => _client.CancelOperationAsync(operationId, serviceUrl, agenticIdentity, customHeaders, cancellationToken);

    /// <summary>
    /// Cancels a batch operation using activity context.
    /// </summary>
    /// <param name="operationId">The ID of the operation to cancel.</param>
    /// <param name="activity">The activity providing service URL and identity context.</param>
    /// <param name="customHeaders">Optional custom headers to include in the request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task CancelAsync(
        string operationId,
        TeamsActivity activity,
        CustomHeaders? customHeaders = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);
        return _client.CancelOperationAsync(
            operationId,
            activity.ServiceUrl!,
            activity.From.GetAgenticIdentity(),
            customHeaders,
            cancellationToken);
    }
}
