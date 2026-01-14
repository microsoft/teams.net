// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net.Mime;
using System.Text;
using System.Text.Json;
using Microsoft.Bot.Core;
using Microsoft.Bot.Core.Hosting;
using Microsoft.Bot.Core.Schema;
using Microsoft.Extensions.Logging;
using Microsoft.Teams.BotApps.Schema;

namespace Microsoft.Teams.BotApps;

using CustomHeaders = Dictionary<string, string>;

/// <summary>
/// Provides methods for interacting with Teams-specific APIs.
/// </summary>
/// <param name="httpClient">The HTTP client instance used to send requests to the Teams service. Must not be null.</param>
/// <param name="logger">The logger instance used for logging. Optional.</param>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1848:Use the LoggerMessage delegates", Justification = "<Pending>")]
public class TeamsAPXClient(HttpClient httpClient, ILogger<TeamsAPXClient> logger = default!)
{
    internal const string TeamsHttpClientName = "TeamsAPXClient";

    /// <summary>
    /// Gets the default custom headers that will be included in all requests.
    /// </summary>
    public CustomHeaders DefaultCustomHeaders { get; } = [];

    #region Team Operations

    /// <summary>
    /// Fetches the list of channels for a given team.
    /// </summary>
    /// <param name="teamId">The ID of the team. Cannot be null or whitespace.</param>
    /// <param name="serviceUrl">The service URL for the Teams service. Cannot be null.</param>
    /// <param name="agenticIdentity">Optional agentic identity for authentication.</param>
    /// <param name="customHeaders">Optional custom headers to include in the request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the list of channels.</returns>
    /// <exception cref="HttpRequestException">Thrown if the channel list could not be retrieved successfully.</exception>
    public async Task<ChannelList> FetchChannelListAsync(string teamId, Uri serviceUrl, AgenticIdentity? agenticIdentity = null, CustomHeaders? customHeaders = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(teamId);
        ArgumentNullException.ThrowIfNull(serviceUrl);

        string url = $"{serviceUrl.ToString().TrimEnd('/')}/v3/teams/{Uri.EscapeDataString(teamId)}/conversations";

        logger?.LogTrace("Fetching channel list from {Url}", url);

        return await SendHttpRequestAsync<ChannelList>(
            HttpMethod.Get,
            url,
            body: null,
            agenticIdentity,
            "fetching channel list",
            customHeaders,
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Fetches details related to a team.
    /// </summary>
    /// <param name="teamId">The ID of the team. Cannot be null or whitespace.</param>
    /// <param name="serviceUrl">The service URL for the Teams service. Cannot be null.</param>
    /// <param name="agenticIdentity">Optional agentic identity for authentication.</param>
    /// <param name="customHeaders">Optional custom headers to include in the request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the team details.</returns>
    /// <exception cref="HttpRequestException">Thrown if the team details could not be retrieved successfully.</exception>
    public async Task<TeamDetails> FetchTeamDetailsAsync(string teamId, Uri serviceUrl, AgenticIdentity? agenticIdentity = null, CustomHeaders? customHeaders = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(teamId);
        ArgumentNullException.ThrowIfNull(serviceUrl);

        string url = $"{serviceUrl.ToString().TrimEnd('/')}/v3/teams/{Uri.EscapeDataString(teamId)}";

        logger?.LogTrace("Fetching team details from {Url}", url);

        return await SendHttpRequestAsync<TeamDetails>(
            HttpMethod.Get,
            url,
            body: null,
            agenticIdentity,
            "fetching team details",
            customHeaders,
            cancellationToken).ConfigureAwait(false);
    }

    #endregion

    #region Meeting Operations

    /// <summary>
    /// Fetches information about a meeting.
    /// </summary>
    /// <param name="meetingId">The ID of the meeting, encoded as a BASE64 string. Cannot be null or whitespace.</param>
    /// <param name="serviceUrl">The service URL for the Teams service. Cannot be null.</param>
    /// <param name="agenticIdentity">Optional agentic identity for authentication.</param>
    /// <param name="customHeaders">Optional custom headers to include in the request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the meeting information.</returns>
    /// <exception cref="HttpRequestException">Thrown if the meeting info could not be retrieved successfully.</exception>
    public async Task<MeetingInfo> FetchMeetingInfoAsync(string meetingId, Uri serviceUrl, AgenticIdentity? agenticIdentity = null, CustomHeaders? customHeaders = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(meetingId);
        ArgumentNullException.ThrowIfNull(serviceUrl);

        string url = $"{serviceUrl.ToString().TrimEnd('/')}/v1/meetings/{Uri.EscapeDataString(meetingId)}";

        logger?.LogTrace("Fetching meeting info from {Url}", url);

        return await SendHttpRequestAsync<MeetingInfo>(
            HttpMethod.Get,
            url,
            body: null,
            agenticIdentity,
            "fetching meeting info",
            customHeaders,
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Fetches details for a meeting participant.
    /// </summary>
    /// <param name="meetingId">The ID of the meeting. Cannot be null or whitespace.</param>
    /// <param name="participantId">The ID of the participant. Cannot be null or whitespace.</param>
    /// <param name="tenantId">The ID of the tenant. Cannot be null or whitespace.</param>
    /// <param name="serviceUrl">The service URL for the Teams service. Cannot be null.</param>
    /// <param name="agenticIdentity">Optional agentic identity for authentication.</param>
    /// <param name="customHeaders">Optional custom headers to include in the request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the participant details.</returns>
    /// <exception cref="HttpRequestException">Thrown if the participant details could not be retrieved successfully.</exception>
    public async Task<MeetingParticipant> FetchParticipantAsync(string meetingId, string participantId, string tenantId, Uri serviceUrl, AgenticIdentity? agenticIdentity = null, CustomHeaders? customHeaders = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(meetingId);
        ArgumentException.ThrowIfNullOrWhiteSpace(participantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentNullException.ThrowIfNull(serviceUrl);

        string url = $"{serviceUrl.ToString().TrimEnd('/')}/v1/meetings/{Uri.EscapeDataString(meetingId)}/participants/{Uri.EscapeDataString(participantId)}?tenantId={Uri.EscapeDataString(tenantId)}";

        logger?.LogTrace("Fetching meeting participant from {Url}", url);

        return await SendHttpRequestAsync<MeetingParticipant>(
            HttpMethod.Get,
            url,
            body: null,
            agenticIdentity,
            "fetching meeting participant",
            customHeaders,
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends a notification to meeting participants.
    /// </summary>
    /// <param name="meetingId">The ID of the meeting. Cannot be null or whitespace.</param>
    /// <param name="notification">The notification to send. Cannot be null.</param>
    /// <param name="serviceUrl">The service URL for the Teams service. Cannot be null.</param>
    /// <param name="agenticIdentity">Optional agentic identity for authentication.</param>
    /// <param name="customHeaders">Optional custom headers to include in the request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains information about failed recipients.</returns>
    /// <exception cref="HttpRequestException">Thrown if the notification could not be sent successfully.</exception>
    public async Task<MeetingNotificationResponse> SendMeetingNotificationAsync(string meetingId, MeetingNotificationBase notification, Uri serviceUrl, AgenticIdentity? agenticIdentity = null, CustomHeaders? customHeaders = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(meetingId);
        ArgumentNullException.ThrowIfNull(notification);
        ArgumentNullException.ThrowIfNull(serviceUrl);

        string url = $"{serviceUrl.ToString().TrimEnd('/')}/v1/meetings/{Uri.EscapeDataString(meetingId)}/notification";
        string body = JsonSerializer.Serialize(notification);

        logger?.LogTrace("Sending meeting notification to {Url}: {Notification}", url, body);

        return await SendHttpRequestAsync<MeetingNotificationResponse>(
            HttpMethod.Post,
            url,
            body,
            agenticIdentity,
            "sending meeting notification",
            customHeaders,
            cancellationToken).ConfigureAwait(false);
    }

    #endregion

    #region Batch Message Operations

    /// <summary>
    /// Sends a message to a list of Teams users.
    /// </summary>
    /// <param name="activity">The activity to send. Cannot be null.</param>
    /// <param name="teamsMembers">The list of team members to send the message to. Cannot be null or empty.</param>
    /// <param name="tenantId">The ID of the tenant. Cannot be null or whitespace.</param>
    /// <param name="serviceUrl">The service URL for the Teams service. Cannot be null.</param>
    /// <param name="agenticIdentity">Optional agentic identity for authentication.</param>
    /// <param name="customHeaders">Optional custom headers to include in the request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the operation ID.</returns>
    /// <exception cref="HttpRequestException">Thrown if the message could not be sent successfully.</exception>
    public async Task<string> SendMessageToListOfUsersAsync(CoreActivity activity, IList<TeamMember> teamsMembers, string tenantId, Uri serviceUrl, AgenticIdentity? agenticIdentity = null, CustomHeaders? customHeaders = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);
        ArgumentNullException.ThrowIfNull(teamsMembers);
        if (teamsMembers.Count == 0)
        {
            throw new ArgumentException("teamsMembers cannot be empty", nameof(teamsMembers));
        }
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentNullException.ThrowIfNull(serviceUrl);

        string url = $"{serviceUrl.ToString().TrimEnd('/')}/v3/batch/conversation/users/";
        var request = new SendMessageToUsersRequest
        {
            Members = teamsMembers,
            Activity = activity,
            TenantId = tenantId
        };
        string body = JsonSerializer.Serialize(request);

        logger?.LogTrace("Sending message to list of users at {Url}: {Request}", url, body);

        return await SendHttpRequestAsync<string>(
            HttpMethod.Post,
            url,
            body,
            agenticIdentity,
            "sending message to list of users",
            customHeaders,
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends a message to all users in a tenant.
    /// </summary>
    /// <param name="activity">The activity to send. Cannot be null.</param>
    /// <param name="tenantId">The ID of the tenant. Cannot be null or whitespace.</param>
    /// <param name="serviceUrl">The service URL for the Teams service. Cannot be null.</param>
    /// <param name="agenticIdentity">Optional agentic identity for authentication.</param>
    /// <param name="customHeaders">Optional custom headers to include in the request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the operation ID.</returns>
    /// <exception cref="HttpRequestException">Thrown if the message could not be sent successfully.</exception>
    public async Task<string> SendMessageToAllUsersInTenantAsync(CoreActivity activity, string tenantId, Uri serviceUrl, AgenticIdentity? agenticIdentity = null, CustomHeaders? customHeaders = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentNullException.ThrowIfNull(serviceUrl);

        string url = $"{serviceUrl.ToString().TrimEnd('/')}/v3/batch/conversation/tenant/";
        var request = new SendMessageToTenantRequest
        {
            Activity = activity,
            TenantId = tenantId
        };
        string body = JsonSerializer.Serialize(request);

        logger?.LogTrace("Sending message to all users in tenant at {Url}: {Request}", url, body);

        return await SendHttpRequestAsync<string>(
            HttpMethod.Post,
            url,
            body,
            agenticIdentity,
            "sending message to all users in tenant",
            customHeaders,
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends a message to all users in a team.
    /// </summary>
    /// <param name="activity">The activity to send. Cannot be null.</param>
    /// <param name="teamId">The ID of the team. Cannot be null or whitespace.</param>
    /// <param name="tenantId">The ID of the tenant. Cannot be null or whitespace.</param>
    /// <param name="serviceUrl">The service URL for the Teams service. Cannot be null.</param>
    /// <param name="agenticIdentity">Optional agentic identity for authentication.</param>
    /// <param name="customHeaders">Optional custom headers to include in the request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the operation ID.</returns>
    /// <exception cref="HttpRequestException">Thrown if the message could not be sent successfully.</exception>
    public async Task<string> SendMessageToAllUsersInTeamAsync(CoreActivity activity, string teamId, string tenantId, Uri serviceUrl, AgenticIdentity? agenticIdentity = null, CustomHeaders? customHeaders = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);
        ArgumentException.ThrowIfNullOrWhiteSpace(teamId);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentNullException.ThrowIfNull(serviceUrl);

        string url = $"{serviceUrl.ToString().TrimEnd('/')}/v3/batch/conversation/team/";
        var request = new SendMessageToTeamRequest
        {
            Activity = activity,
            TeamId = teamId,
            TenantId = tenantId
        };
        string body = JsonSerializer.Serialize(request);

        logger?.LogTrace("Sending message to all users in team at {Url}: {Request}", url, body);

        return await SendHttpRequestAsync<string>(
            HttpMethod.Post,
            url,
            body,
            agenticIdentity,
            "sending message to all users in team",
            customHeaders,
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends a message to a list of Teams channels.
    /// </summary>
    /// <param name="activity">The activity to send. Cannot be null.</param>
    /// <param name="channelMembers">The list of channels to send the message to. Cannot be null or empty.</param>
    /// <param name="tenantId">The ID of the tenant. Cannot be null or whitespace.</param>
    /// <param name="serviceUrl">The service URL for the Teams service. Cannot be null.</param>
    /// <param name="agenticIdentity">Optional agentic identity for authentication.</param>
    /// <param name="customHeaders">Optional custom headers to include in the request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the operation ID.</returns>
    /// <exception cref="HttpRequestException">Thrown if the message could not be sent successfully.</exception>
    public async Task<string> SendMessageToListOfChannelsAsync(CoreActivity activity, IList<TeamMember> channelMembers, string tenantId, Uri serviceUrl, AgenticIdentity? agenticIdentity = null, CustomHeaders? customHeaders = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);
        ArgumentNullException.ThrowIfNull(channelMembers);
        if (channelMembers.Count == 0)
        {
            throw new ArgumentException("channelMembers cannot be empty", nameof(channelMembers));
        }
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentNullException.ThrowIfNull(serviceUrl);

        string url = $"{serviceUrl.ToString().TrimEnd('/')}/v3/batch/conversation/channels/";
        var request = new SendMessageToUsersRequest
        {
            Members = channelMembers,
            Activity = activity,
            TenantId = tenantId
        };
        string body = JsonSerializer.Serialize(request);

        logger?.LogTrace("Sending message to list of channels at {Url}: {Request}", url, body);

        return await SendHttpRequestAsync<string>(
            HttpMethod.Post,
            url,
            body,
            agenticIdentity,
            "sending message to list of channels",
            customHeaders,
            cancellationToken).ConfigureAwait(false);
    }

    #endregion

    #region Batch Operation Management

    /// <summary>
    /// Gets the state of a batch operation.
    /// </summary>
    /// <param name="operationId">The ID of the operation. Cannot be null or whitespace.</param>
    /// <param name="serviceUrl">The service URL for the Teams service. Cannot be null.</param>
    /// <param name="agenticIdentity">Optional agentic identity for authentication.</param>
    /// <param name="customHeaders">Optional custom headers to include in the request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the operation state.</returns>
    /// <exception cref="HttpRequestException">Thrown if the operation state could not be retrieved successfully.</exception>
    public async Task<BatchOperationState> GetOperationStateAsync(string operationId, Uri serviceUrl, AgenticIdentity? agenticIdentity = null, CustomHeaders? customHeaders = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        ArgumentNullException.ThrowIfNull(serviceUrl);

        string url = $"{serviceUrl.ToString().TrimEnd('/')}/v3/batch/conversation/{Uri.EscapeDataString(operationId)}";

        logger?.LogTrace("Getting operation state from {Url}", url);

        return await SendHttpRequestAsync<BatchOperationState>(
            HttpMethod.Get,
            url,
            body: null,
            agenticIdentity,
            "getting operation state",
            customHeaders,
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the failed entries of a batch operation with error code and message.
    /// </summary>
    /// <param name="operationId">The ID of the operation. Cannot be null or whitespace.</param>
    /// <param name="serviceUrl">The service URL for the Teams service. Cannot be null.</param>
    /// <param name="continuationToken">Optional continuation token for pagination.</param>
    /// <param name="agenticIdentity">Optional agentic identity for authentication.</param>
    /// <param name="customHeaders">Optional custom headers to include in the request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the failed entries.</returns>
    /// <exception cref="HttpRequestException">Thrown if the failed entries could not be retrieved successfully.</exception>
    public async Task<BatchFailedEntriesResponse> GetPagedFailedEntriesAsync(string operationId, Uri serviceUrl, string? continuationToken = null, AgenticIdentity? agenticIdentity = null, CustomHeaders? customHeaders = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        ArgumentNullException.ThrowIfNull(serviceUrl);

        string url = $"{serviceUrl.ToString().TrimEnd('/')}/v3/batch/conversation/failedentries/{Uri.EscapeDataString(operationId)}";

        if (!string.IsNullOrWhiteSpace(continuationToken))
        {
            url += $"?continuationToken={Uri.EscapeDataString(continuationToken)}";
        }

        logger?.LogTrace("Getting paged failed entries from {Url}", url);

        return await SendHttpRequestAsync<BatchFailedEntriesResponse>(
            HttpMethod.Get,
            url,
            body: null,
            agenticIdentity,
            "getting paged failed entries",
            customHeaders,
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Cancels a batch operation by its ID.
    /// </summary>
    /// <param name="operationId">The ID of the operation to cancel. Cannot be null or whitespace.</param>
    /// <param name="serviceUrl">The service URL for the Teams service. Cannot be null.</param>
    /// <param name="agenticIdentity">Optional agentic identity for authentication.</param>
    /// <param name="customHeaders">Optional custom headers to include in the request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="HttpRequestException">Thrown if the operation could not be cancelled successfully.</exception>
    public async Task CancelOperationAsync(string operationId, Uri serviceUrl, AgenticIdentity? agenticIdentity = null, CustomHeaders? customHeaders = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        ArgumentNullException.ThrowIfNull(serviceUrl);

        string url = $"{serviceUrl.ToString().TrimEnd('/')}/v3/batch/conversation/{Uri.EscapeDataString(operationId)}";

        logger?.LogTrace("Cancelling operation at {Url}", url);

        await SendHttpRequestAsync<object>(
            HttpMethod.Delete,
            url,
            body: null,
            agenticIdentity,
            "cancelling operation",
            customHeaders,
            cancellationToken).ConfigureAwait(false);
    }

    #endregion

    #region Private Methods

    private async Task<T> SendHttpRequestAsync<T>(HttpMethod method, string url, string? body, AgenticIdentity? agenticIdentity, string operationDescription, CustomHeaders? customHeaders, CancellationToken cancellationToken)
    {
        using HttpRequestMessage request = new(method, url);

        if (body is not null)
        {
            request.Content = new StringContent(body, Encoding.UTF8, MediaTypeNames.Application.Json);
        }

        if (agenticIdentity is not null)
        {
            request.Options.Set(BotAuthenticationHandler.AgenticIdentityKey, agenticIdentity);
        }

        // Apply default custom headers
        foreach (KeyValuePair<string, string> header in DefaultCustomHeaders)
        {
            request.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        // Apply method-level custom headers (these override default headers if same key)
        if (customHeaders is not null)
        {
            foreach (KeyValuePair<string, string> header in customHeaders)
            {
                request.Headers.Remove(header.Key);
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        logger?.LogTrace("Sending HTTP {Method} request to {Url} with body: {Body}", method, url, body);

        using HttpResponseMessage resp = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

        if (resp.IsSuccessStatusCode)
        {
            string responseString = await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            if (responseString.Length > 2) // to handle empty response
            {
                // Handle string responses (like operation IDs)
                if (typeof(T) == typeof(string))
                {
                    // Try to deserialize as a quoted string first, then return raw if that fails
                    try
                    {
                        T? result = JsonSerializer.Deserialize<T>(responseString);
                        return result ?? (T)(object)responseString;
                    }
                    catch (JsonException)
                    {
                        return (T)(object)responseString;
                    }
                }

                T? deserializedResult = JsonSerializer.Deserialize<T>(responseString);
                return deserializedResult ?? throw new InvalidOperationException($"Failed to deserialize response for {operationDescription}");
            }
            // Empty response - return default value (e.g., for DELETE operations)
            return default!;
        }
        else
        {
            string errResponseString = await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            throw new HttpRequestException($"Error {operationDescription} {resp.StatusCode}. {errResponseString}");
        }
    }

    #endregion
}
