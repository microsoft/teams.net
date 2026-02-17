// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Bot.Apps.Schema;
using Microsoft.Teams.Bot.Core.Schema;

namespace Microsoft.Teams.Bot.Apps.Api;

using CustomHeaders = Dictionary<string, string>;

/// <summary>
/// Provides meeting operations for managing Teams meetings.
/// </summary>
public class MeetingsApi
{
    private readonly TeamsApiClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="MeetingsApi"/> class.
    /// </summary>
    /// <param name="teamsApiClient">The Teams API client for meeting operations.</param>
    internal MeetingsApi(TeamsApiClient teamsApiClient)
    {
        _client = teamsApiClient;
    }

    /// <summary>
    /// Gets information about a meeting.
    /// </summary>
    /// <param name="meetingId">The ID of the meeting, encoded as a BASE64 string.</param>
    /// <param name="serviceUrl">The service URL for the Teams service.</param>
    /// <param name="agenticIdentity">Optional agentic identity for authentication.</param>
    /// <param name="customHeaders">Optional custom headers to include in the request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the meeting information.</returns>
    public Task<MeetingInfo> GetByIdAsync(
        string meetingId,
        Uri serviceUrl,
        AgenticIdentity? agenticIdentity = null,
        CustomHeaders? customHeaders = null,
        CancellationToken cancellationToken = default)
        => _client.FetchMeetingInfoAsync(meetingId, serviceUrl, agenticIdentity, customHeaders, cancellationToken);

    /// <summary>
    /// Gets information about a meeting using activity context.
    /// </summary>
    /// <param name="meetingId">The ID of the meeting, encoded as a BASE64 string.</param>
    /// <param name="activity">The activity providing service URL and identity context.</param>
    /// <param name="customHeaders">Optional custom headers to include in the request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the meeting information.</returns>
    public Task<MeetingInfo> GetByIdAsync(
        string meetingId,
        TeamsActivity activity,
        CustomHeaders? customHeaders = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);
        return _client.FetchMeetingInfoAsync(
            meetingId,
            activity.ServiceUrl!,
            activity.From.GetAgenticIdentity(),
            customHeaders,
            cancellationToken);
    }

    /// <summary>
    /// Gets details for a meeting participant.
    /// </summary>
    /// <param name="meetingId">The ID of the meeting.</param>
    /// <param name="participantId">The ID of the participant.</param>
    /// <param name="tenantId">The ID of the tenant.</param>
    /// <param name="serviceUrl">The service URL for the Teams service.</param>
    /// <param name="agenticIdentity">Optional agentic identity for authentication.</param>
    /// <param name="customHeaders">Optional custom headers to include in the request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the participant details.</returns>
    public Task<MeetingParticipant> GetParticipantAsync(
        string meetingId,
        string participantId,
        string tenantId,
        Uri serviceUrl,
        AgenticIdentity? agenticIdentity = null,
        CustomHeaders? customHeaders = null,
        CancellationToken cancellationToken = default)
        => _client.FetchParticipantAsync(meetingId, participantId, tenantId, serviceUrl, agenticIdentity, customHeaders, cancellationToken);

    /// <summary>
    /// Gets details for a meeting participant using activity context.
    /// </summary>
    /// <param name="meetingId">The ID of the meeting.</param>
    /// <param name="participantId">The ID of the participant.</param>
    /// <param name="activity">The activity providing service URL, tenant ID, and identity context.</param>
    /// <param name="customHeaders">Optional custom headers to include in the request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the participant details.</returns>
    public Task<MeetingParticipant> GetParticipantAsync(
        string meetingId,
        string participantId,
        TeamsActivity activity,
        CustomHeaders? customHeaders = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);
        return _client.FetchParticipantAsync(
            meetingId,
            participantId,
            activity.ChannelData?.Tenant?.Id ?? throw new InvalidOperationException("Tenant ID not available in activity"),
            activity.ServiceUrl!,
            activity.From.GetAgenticIdentity(),
            customHeaders,
            cancellationToken);
    }

    /// <summary>
    /// Sends a notification to meeting participants.
    /// </summary>
    /// <param name="meetingId">The ID of the meeting.</param>
    /// <param name="notification">The notification to send.</param>
    /// <param name="serviceUrl">The service URL for the Teams service.</param>
    /// <param name="agenticIdentity">Optional agentic identity for authentication.</param>
    /// <param name="customHeaders">Optional custom headers to include in the request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains information about failed recipients.</returns>
    public Task<MeetingNotificationResponse> SendNotificationAsync(
        string meetingId,
        TargetedMeetingNotification notification,
        Uri serviceUrl,
        AgenticIdentity? agenticIdentity = null,
        CustomHeaders? customHeaders = null,
        CancellationToken cancellationToken = default)
        => _client.SendMeetingNotificationAsync(meetingId, notification, serviceUrl, agenticIdentity, customHeaders, cancellationToken);

    /// <summary>
    /// Sends a notification to meeting participants using activity context.
    /// </summary>
    /// <param name="meetingId">The ID of the meeting.</param>
    /// <param name="notification">The notification to send.</param>
    /// <param name="activity">The activity providing service URL and identity context.</param>
    /// <param name="customHeaders">Optional custom headers to include in the request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains information about failed recipients.</returns>
    public Task<MeetingNotificationResponse> SendNotificationAsync(
        string meetingId,
        TargetedMeetingNotification notification,
        TeamsActivity activity,
        CustomHeaders? customHeaders = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);
        return _client.SendMeetingNotificationAsync(
            meetingId,
            notification,
            activity.ServiceUrl!,
            activity.From.GetAgenticIdentity(),
            customHeaders,
            cancellationToken);
    }
}
