// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Bot.Builder;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using Microsoft.Teams.Bot.Apps;
using Microsoft.Teams.Bot.Core;
using Microsoft.Teams.Bot.Core.Schema;
using BotFrameworkTeams = Microsoft.Bot.Schema.Teams;
using AppsTeams = Microsoft.Teams.Bot.Apps;
using Microsoft.Bot.Schema.Teams;

namespace Microsoft.Teams.Bot.Compat;

/// <summary>
/// Provides utility methods for the events and interactions that occur within Microsoft Teams.
/// This class adapts the Teams Bot Core SDK to the Bot Framework v4 SDK TeamsInfo API.
/// </summary>
public static class CompatTeamsInfo
{
    #region Helper Methods

    private static readonly System.Text.Json.JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static ConversationClient GetConversationClient(ITurnContext turnContext)
    {
        var connectorClient = turnContext.TurnState.Get<IConnectorClient>()
            ?? throw new InvalidOperationException("This method requires a connector client.");

        if (connectorClient is CompatConnectorClient compatClient)
        {
            return ((CompatConversations)compatClient.Conversations)._client;
        }

        throw new InvalidOperationException("Connector client is not compatible.");
    }

    private static TeamsApiClient GetTeamsApiClient(ITurnContext turnContext)
    {
        return turnContext.TurnState.Get<TeamsApiClient>()
            ?? throw new InvalidOperationException("This method requires TeamsApiClient.");
    }

    private static string GetServiceUrl(ITurnContext turnContext)
    {
        return turnContext.Activity.ServiceUrl
            ?? throw new InvalidOperationException("ServiceUrl is required.");
    }

    private static AgenticIdentity GetIdentity(ITurnContext turnContext)
    {
        var coreActivity = ((Activity)turnContext.Activity).FromCompatActivity();
        return AgenticIdentity.FromProperties(coreActivity.From.Properties) ?? new AgenticIdentity();
    }

    #endregion

    #region Member & Participant Methods

    /// <summary>
    /// Gets the account of a single conversation member.
    /// This works in one-on-one, group, and teams scoped conversations.
    /// </summary>
    /// <param name="turnContext">Turn context.</param>
    /// <param name="userId">ID of the user in question.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The member's channel account information.</returns>
    public static async Task<BotFrameworkTeams.TeamsChannelAccount> GetMemberAsync(
        ITurnContext turnContext,
        string userId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(turnContext);
        var teamInfo = turnContext.Activity.TeamsGetTeamInfo();

        if (teamInfo?.Id != null)
        {
            return await GetTeamMemberAsync(turnContext, userId, teamInfo.Id, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            var conversationId = turnContext.Activity?.Conversation?.Id
                ?? throw new InvalidOperationException("The GetMember operation needs a valid conversation Id.");

            if (userId == null)
            {
                throw new InvalidOperationException("The GetMember operation needs a valid user Id.");
            }

            var client = GetConversationClient(turnContext);
            var serviceUrl = new Uri(GetServiceUrl(turnContext));
            var identity = GetIdentity(turnContext);

            var result = await client.GetConversationMemberAsync<Microsoft.Teams.Bot.Apps.Schema.TeamsConversationAccount>(
                conversationId, userId, serviceUrl, identity, null, cancellationToken).ConfigureAwait(false);

            return result.ToCompatTeamsChannelAccount();
        }
    }

    /// <summary>
    /// Gets the conversation members of a one-on-one or group chat.
    /// </summary>
    /// <param name="turnContext">Turn context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of channel accounts.</returns>
    [Obsolete("Microsoft Teams is deprecating the non-paged version of the getMembers API which this method uses. Please use GetPagedMembersAsync instead of this API.")]
    public static async Task<IEnumerable<BotFrameworkTeams.TeamsChannelAccount>> GetMembersAsync(
        ITurnContext turnContext,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(turnContext);
        var teamInfo = turnContext.Activity.TeamsGetTeamInfo();

        if (teamInfo?.Id != null)
        {
            return await GetTeamMembersAsync(turnContext, teamInfo.Id, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            var conversationId = turnContext.Activity?.Conversation?.Id
                ?? throw new InvalidOperationException("The GetMembers operation needs a valid conversation Id.");

            var client = GetConversationClient(turnContext);
            var serviceUrl = new Uri(GetServiceUrl(turnContext));
            var identity = GetIdentity(turnContext);

            var members = await client.GetConversationMembersAsync(
                conversationId, serviceUrl, identity, null, cancellationToken).ConfigureAwait(false);

            return members.Select(m => m.ToCompatTeamsChannelAccount());
        }
    }

    /// <summary>
    /// Gets a paginated list of members of one-on-one, group, or team conversation.
    /// </summary>
    /// <param name="turnContext">Turn context.</param>
    /// <param name="pageSize">Suggested number of entries on a page.</param>
    /// <param name="continuationToken">Continuation token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paged members result.</returns>
    public static async Task<BotFrameworkTeams.TeamsPagedMembersResult> GetPagedMembersAsync(
        ITurnContext turnContext,
        int? pageSize = default,
        string? continuationToken = default,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(turnContext);
        var teamInfo = turnContext.Activity.TeamsGetTeamInfo();

        if (teamInfo?.Id != null)
        {
            return await GetPagedTeamMembersAsync(turnContext, teamInfo.Id, continuationToken, pageSize, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            var conversationId = turnContext.Activity?.Conversation?.Id
                ?? throw new InvalidOperationException("The GetMembers operation needs a valid conversation Id.");

            var client = GetConversationClient(turnContext);
            var serviceUrl = new Uri(GetServiceUrl(turnContext));
            var identity = GetIdentity(turnContext);

            var pagedMembers = await client.GetConversationPagedMembersAsync(
                conversationId, serviceUrl, pageSize, continuationToken, identity, null, cancellationToken).ConfigureAwait(false);

            return pagedMembers.ToCompatTeamsPagedMembersResult();
        }
    }

    /// <summary>
    /// Gets the member of a teams scoped conversation.
    /// </summary>
    /// <param name="turnContext">Turn context.</param>
    /// <param name="userId">User id.</param>
    /// <param name="teamId">ID of the Teams team.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Team member's channel account.</returns>
    public static async Task<BotFrameworkTeams.TeamsChannelAccount> GetTeamMemberAsync(
        ITurnContext turnContext,
        string userId,
        string? teamId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(turnContext);
        var t = teamId ?? turnContext.Activity.TeamsGetTeamInfo()?.Id
            ?? throw new InvalidOperationException("This method is only valid within the scope of MS Teams Team.");

        if (userId == null)
        {
            throw new InvalidOperationException("The GetMember operation needs a valid user Id.");
        }

        var client = GetConversationClient(turnContext);
        var serviceUrl = new Uri(GetServiceUrl(turnContext));
        var identity = GetIdentity(turnContext);

        var result = await client.GetConversationMemberAsync<Microsoft.Teams.Bot.Apps.Schema.TeamsConversationAccount>(
            t, userId, serviceUrl, identity, null, cancellationToken).ConfigureAwait(false);

        return result.ToCompatTeamsChannelAccount();
    }

    /// <summary>
    /// Gets the list of BotFrameworkTeams.TeamsChannelAccounts within a team.
    /// This only works in teams scoped conversations.
    /// </summary>
    /// <param name="turnContext">Turn context.</param>
    /// <param name="teamId">ID of the Teams team.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of team members.</returns>
    [Obsolete("Microsoft Teams is deprecating the non-paged version of the getMembers API which this method uses. Please use GetPagedTeamMembersAsync instead of this API.")]
    public static async Task<IEnumerable<BotFrameworkTeams.TeamsChannelAccount>> GetTeamMembersAsync(
        ITurnContext turnContext,
        string? teamId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(turnContext);
        var t = teamId ?? turnContext.Activity.TeamsGetTeamInfo()?.Id
            ?? throw new InvalidOperationException("This method is only valid within the scope of MS Teams Team.");

        var client = GetConversationClient(turnContext);
        var serviceUrl = new Uri(GetServiceUrl(turnContext));
        var identity = GetIdentity(turnContext);

        var members = await client.GetConversationMembersAsync(
            t, serviceUrl, identity, null, cancellationToken).ConfigureAwait(false);

        return members.Select(m => m.ToCompatTeamsChannelAccount());
    }

    /// <summary>
    /// Gets a paginated list of members of a team.
    /// This only works in teams scoped conversations.
    /// </summary>
    /// <param name="turnContext">Turn context.</param>
    /// <param name="teamId">ID of the Teams team.</param>
    /// <param name="continuationToken">Continuation token.</param>
    /// <param name="pageSize">Number of entries on the page.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paged team members result.</returns>
    public static async Task<BotFrameworkTeams.TeamsPagedMembersResult> GetPagedTeamMembersAsync(
        ITurnContext turnContext,
        string? teamId = null,
        string? continuationToken = default,
        int? pageSize = default,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(turnContext);
        var t = teamId ?? turnContext.Activity.TeamsGetTeamInfo()?.Id
            ?? throw new InvalidOperationException("This method is only valid within the scope of MS Teams Team.");

        var client = GetConversationClient(turnContext);
        var serviceUrl = new Uri(GetServiceUrl(turnContext));
        var identity = GetIdentity(turnContext);

        var pagedMembers = await client.GetConversationPagedMembersAsync(
            t, serviceUrl, pageSize, continuationToken, identity, null, cancellationToken).ConfigureAwait(false);

        return pagedMembers.ToCompatTeamsPagedMembersResult();
    }

    #endregion

    #region Meeting Methods

    /// <summary>
    /// Gets the information for the given meeting id.
    /// </summary>
    /// <param name="turnContext">Turn context.</param>
    /// <param name="meetingId">The BASE64-encoded id of the Teams meeting.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Meeting information.</returns>
    public static async Task<BotFrameworkTeams.MeetingInfo> GetMeetingInfoAsync(
        ITurnContext turnContext,
        string? meetingId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(turnContext);
        meetingId ??= turnContext.Activity.TeamsGetMeetingInfo()?.Id
            ?? throw new InvalidOperationException("The meetingId can only be null if turnContext is within the scope of a MS Teams Meeting.");

        var client = GetTeamsApiClient(turnContext);
        var serviceUrl = new Uri(GetServiceUrl(turnContext));
        var identity = GetIdentity(turnContext);

        var result = await client.FetchMeetingInfoAsync(
            meetingId, serviceUrl, identity, null, cancellationToken).ConfigureAwait(false);

        return result.ToCompatMeetingInfo();
    }

    /// <summary>
    /// Gets the details for the given meeting participant. This only works in teams meeting scoped conversations.
    /// </summary>
    /// <param name="turnContext">Turn context.</param>
    /// <param name="meetingId">The id of the Teams meeting. BotFrameworkTeams.TeamsChannelData.Meeting.Id will be used if none provided.</param>
    /// <param name="participantId">The id of the Teams meeting participant. From.AadObjectId will be used if none provided.</param>
    /// <param name="tenantId">The id of the Teams meeting Tenant. BotFrameworkTeams.TeamsChannelData.Tenant.Id will be used if none provided.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Team participant channel account.</returns>
    public static async Task<BotFrameworkTeams.TeamsMeetingParticipant> GetMeetingParticipantAsync(
        ITurnContext turnContext,
        string? meetingId = null,
        string? participantId = null,
        string? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(turnContext);
        meetingId ??= turnContext.Activity.TeamsGetMeetingInfo()?.Id
            ?? throw new InvalidOperationException("This method is only valid within the scope of a MS Teams Meeting.");
        participantId ??= turnContext.Activity.From.AadObjectId
            ?? throw new InvalidOperationException($"{nameof(participantId)} is required.");
        tenantId ??= turnContext.Activity.GetChannelData<BotFrameworkTeams.TeamsChannelData>()?.Tenant?.Id
            ?? throw new InvalidOperationException($"{nameof(tenantId)} is required.");

        var client = GetTeamsApiClient(turnContext);
        var serviceUrl = new Uri(GetServiceUrl(turnContext));
        var identity = GetIdentity(turnContext);

        var result = await client.FetchParticipantAsync(
            meetingId, participantId, tenantId, serviceUrl, identity, null, cancellationToken).ConfigureAwait(false);

        return result.ToCompatTeamsMeetingParticipant();
    }

    /// <summary>
    /// Sends a notification to meeting participants. This functionality is available only in teams meeting scoped conversations.
    /// </summary>
    /// <param name="turnContext">Turn context.</param>
    /// <param name="notification">The notification to send to Teams.</param>
    /// <param name="meetingId">The id of the Teams meeting. BotFrameworkTeams.TeamsChannelData.Meeting.Id will be used if none provided.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Meeting notification response.</returns>
    public static async Task<BotFrameworkTeams.MeetingNotificationResponse> SendMeetingNotificationAsync(
        ITurnContext turnContext,
        BotFrameworkTeams.MeetingNotificationBase? notification,
        string? meetingId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(turnContext);
        meetingId ??= turnContext.Activity.TeamsGetMeetingInfo()?.Id
            ?? throw new InvalidOperationException("This method is only valid within the scope of a MS Teams Meeting.");
        notification = notification ?? throw new InvalidOperationException($"{nameof(notification)} is required.");

        var client = GetTeamsApiClient(turnContext);
        var serviceUrl = new Uri(GetServiceUrl(turnContext));
        var identity = GetIdentity(turnContext);

        // Convert Bot Framework MeetingNotificationBase to Core MeetingNotificationBase using JSON round-trip
        var json = Newtonsoft.Json.JsonConvert.SerializeObject(notification);
        var coreNotification = System.Text.Json.JsonSerializer.Deserialize<AppsTeams.TargetedMeetingNotification>(json, s_jsonOptions);


        var result = await client.SendMeetingNotificationAsync(
            meetingId, coreNotification!, serviceUrl, identity, null, cancellationToken).ConfigureAwait(false);

        return result.ToCompatMeetingNotificationResponse();
    }

    #endregion

    #region Team & Channel Methods

    /// <summary>
    /// Gets the details for the given team id. This only works in teams scoped conversations.
    /// </summary>
    /// <param name="turnContext">Turn context.</param>
    /// <param name="teamId">The id of the Teams team.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Team details.</returns>
    public static async Task<BotFrameworkTeams.TeamDetails> GetTeamDetailsAsync(
        ITurnContext turnContext,
        string? teamId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(turnContext);
        var t = teamId ?? turnContext.Activity.TeamsGetTeamInfo()?.Id
            ?? throw new InvalidOperationException("This method is only valid within the scope of MS Teams Team.");

        var client = GetTeamsApiClient(turnContext);
        var serviceUrl = new Uri(GetServiceUrl(turnContext));
        var identity = GetIdentity(turnContext);

        var result = await client.FetchTeamDetailsAsync(
            t, serviceUrl, identity, null, cancellationToken).ConfigureAwait(false);

        return result.ToCompatTeamDetails();
    }

    /// <summary>
    /// Returns a list of channels in a Team.
    /// This only works in teams scoped conversations.
    /// </summary>
    /// <param name="turnContext">Turn context.</param>
    /// <param name="teamId">ID of the Teams team.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of channel information.</returns>
    public static async Task<IList<BotFrameworkTeams.ChannelInfo>> GetTeamChannelsAsync(
        ITurnContext turnContext,
        string? teamId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(turnContext);
        var t = teamId ?? turnContext.Activity.TeamsGetTeamInfo()?.Id
            ?? throw new InvalidOperationException("This method is only valid within the scope of MS Teams Team.");

        var client = GetTeamsApiClient(turnContext);
        var serviceUrl = new Uri(GetServiceUrl(turnContext));
        var identity = GetIdentity(turnContext);

        var channelList = await client.FetchChannelListAsync(
            t, serviceUrl, identity, null, cancellationToken).ConfigureAwait(false);

        return channelList.Channels?.Select(c => c.ToCompatChannelInfo()).ToList() ?? [];
    }

    #endregion

    #region Batch Messaging Methods

    /// <summary>
    /// Sends a message to the provided list of Teams members.
    /// </summary>
    /// <param name="turnContext">Turn context.</param>
    /// <param name="activity">The activity to send.</param>
    /// <param name="teamsMembers">The list of members.</param>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The operation Id.</returns>
    public static async Task<string> SendMessageToListOfUsersAsync(
        ITurnContext turnContext,
        IActivity activity,
        IList<BotFrameworkTeams.TeamMember> teamsMembers,
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(turnContext);
        activity = activity ?? throw new InvalidOperationException($"{nameof(activity)} is required.");
        teamsMembers = teamsMembers ?? throw new InvalidOperationException($"{nameof(teamsMembers)} is required.");
        tenantId = tenantId ?? throw new InvalidOperationException($"{nameof(tenantId)} is required.");

        var client = GetTeamsApiClient(turnContext);
        var serviceUrl = new Uri(GetServiceUrl(turnContext));
        var identity = GetIdentity(turnContext);
        var coreActivity = ((Activity)activity).FromCompatActivity();

        var coreTeamsMembers = teamsMembers.Select(m => m.FromCompatTeamMember()).ToList();

        return await client.SendMessageToListOfUsersAsync(
            coreActivity, coreTeamsMembers, tenantId, serviceUrl, identity, null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends a message to the provided list of Teams channels.
    /// </summary>
    /// <param name="turnContext">Turn context.</param>
    /// <param name="activity">The activity to send.</param>
    /// <param name="channelsMembers">The list of channels.</param>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The operation Id.</returns>
    public static async Task<string> SendMessageToListOfChannelsAsync(
        ITurnContext turnContext,
        IActivity activity,
        IList<BotFrameworkTeams.TeamMember> channelsMembers,
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(turnContext);
        activity = activity ?? throw new InvalidOperationException($"{nameof(activity)} is required.");
        channelsMembers = channelsMembers ?? throw new InvalidOperationException($"{nameof(channelsMembers)} is required.");
        tenantId = tenantId ?? throw new InvalidOperationException($"{nameof(tenantId)} is required.");

        var client = GetTeamsApiClient(turnContext);
        var serviceUrl = new Uri(GetServiceUrl(turnContext));
        var identity = GetIdentity(turnContext);
        var coreActivity = ((Activity)activity).FromCompatActivity();

        var coreChannelsMembers = channelsMembers.Select(m => m.FromCompatTeamMember()).ToList();

        return await client.SendMessageToListOfChannelsAsync(
            coreActivity, coreChannelsMembers, tenantId, serviceUrl, identity, null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends a message to all the users in a team.
    /// </summary>
    /// <param name="turnContext">The turn context.</param>
    /// <param name="activity">The activity to send to the users in the team.</param>
    /// <param name="teamId">The team ID.</param>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The operation Id.</returns>
    public static async Task<string> SendMessageToAllUsersInTeamAsync(
        ITurnContext turnContext,
        IActivity activity,
        string teamId,
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(turnContext);
        activity = activity ?? throw new InvalidOperationException($"{nameof(activity)} is required.");
        teamId = teamId ?? throw new InvalidOperationException($"{nameof(teamId)} is required.");
        tenantId = tenantId ?? throw new InvalidOperationException($"{nameof(tenantId)} is required.");

        var client = GetTeamsApiClient(turnContext);
        var serviceUrl = new Uri(GetServiceUrl(turnContext));
        var identity = GetIdentity(turnContext);
        var coreActivity = ((Activity)activity).FromCompatActivity();

        return await client.SendMessageToAllUsersInTeamAsync(
            coreActivity, teamId, tenantId, serviceUrl, identity, null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends a message to all the users in a tenant.
    /// </summary>
    /// <param name="turnContext">The turn context.</param>
    /// <param name="activity">The activity to send to the tenant.</param>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The operation Id.</returns>
    public static async Task<string> SendMessageToAllUsersInTenantAsync(
        ITurnContext turnContext,
        IActivity activity,
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(turnContext);
        activity = activity ?? throw new InvalidOperationException($"{nameof(activity)} is required.");
        tenantId = tenantId ?? throw new InvalidOperationException($"{nameof(tenantId)} is required.");

        var client = GetTeamsApiClient(turnContext);
        var serviceUrl = new Uri(GetServiceUrl(turnContext));
        var identity = GetIdentity(turnContext);
        var coreActivity = ((Activity)activity).FromCompatActivity();

        return await client.SendMessageToAllUsersInTenantAsync(
            coreActivity, tenantId, serviceUrl, identity, null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Creates a new thread in a team chat and sends an activity to that new thread.
    /// Use this method if you are using CloudAdapter where credentials are handled by the adapter.
    /// </summary>
    /// <param name="turnContext">Turn context.</param>
    /// <param name="activity">The activity to send on starting the new thread.</param>
    /// <param name="teamsChannelId">The Team's Channel ID, note this is distinct from the Bot Framework activity property with same name.</param>
    /// <param name="botAppId">The bot's appId.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Tuple with conversation reference and activity id.</returns>
    public static async Task<Tuple<ConversationReference, string>> SendMessageToTeamsChannelAsync(
        ITurnContext turnContext,
        IActivity activity,
        string teamsChannelId,
        string botAppId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(turnContext);

        if (turnContext.Activity == null)
        {
            throw new InvalidOperationException(nameof(turnContext.Activity));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(teamsChannelId);

        ConversationReference? conversationReference = null;
        var newActivityId = string.Empty;
        var serviceUrl = turnContext.Activity.ServiceUrl;
        var conversationParameters = new Microsoft.Bot.Schema.ConversationParameters
        {
            IsGroup = true,
            ChannelData = new BotFrameworkTeams.TeamsChannelData { Channel = new BotFrameworkTeams.ChannelInfo { Id = teamsChannelId } },
            Activity = (Activity)activity,
        };

        await turnContext.Adapter.CreateConversationAsync(
            botAppId,
            Channels.Msteams,
            serviceUrl,
            null,
            conversationParameters,
            (t, ct) =>
            {
                conversationReference = t.Activity.GetConversationReference();
                newActivityId = t.Activity.Id;
                return Task.CompletedTask;
            },
            cancellationToken).ConfigureAwait(false);

        return new Tuple<ConversationReference, string>(conversationReference!, newActivityId);
    }

    #endregion

    #region Batch Operation Management

    /// <summary>
    /// Gets the state of an operation.
    /// </summary>
    /// <param name="turnContext">Turn context.</param>
    /// <param name="operationId">The operationId to get the state of.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The state and responses of the operation.</returns>
    public static async Task<BotFrameworkTeams.BatchOperationState> GetOperationStateAsync(
        ITurnContext turnContext,
        string operationId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(turnContext);
        operationId = operationId ?? throw new InvalidOperationException($"{nameof(operationId)} is required.");

        var client = GetTeamsApiClient(turnContext);
        var serviceUrl = new Uri(GetServiceUrl(turnContext));
        var identity = GetIdentity(turnContext);

        var result = await client.GetOperationStateAsync(
            operationId, serviceUrl, identity, null, cancellationToken).ConfigureAwait(false);

        return result.ToCompatBatchOperationState();
    }

    /// <summary>
    /// Gets the failed entries of a batch operation.
    /// </summary>
    /// <param name="turnContext">The turn context.</param>
    /// <param name="operationId">The operationId to get the failed entries of.</param>
    /// <param name="continuationToken">The continuation token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The list of failed entries of the operation.</returns>
    public static async Task<BotFrameworkTeams.BatchFailedEntriesResponse> GetPagedFailedEntriesAsync(
        ITurnContext turnContext,
        string operationId,
        string? continuationToken = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(turnContext);
        operationId = operationId ?? throw new InvalidOperationException($"{nameof(operationId)} is required.");

        var client = GetTeamsApiClient(turnContext);
        var serviceUrl = new Uri(GetServiceUrl(turnContext));
        var identity = GetIdentity(turnContext);

        var result = await client.GetPagedFailedEntriesAsync(
            operationId, serviceUrl, continuationToken, identity, null, cancellationToken).ConfigureAwait(false);

        return result.ToCompatBatchFailedEntriesResponse();
    }

    /// <summary>
    /// Cancels a batch operation by its id.
    /// </summary>
    /// <param name="turnContext">The turn context.</param>
    /// <param name="operationId">The id of the operation to cancel.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task CancelOperationAsync(
        ITurnContext turnContext,
        string operationId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(turnContext);
        operationId = operationId ?? throw new InvalidOperationException($"{nameof(operationId)} is required.");

        var client = GetTeamsApiClient(turnContext);
        var serviceUrl = new Uri(GetServiceUrl(turnContext));
        var identity = GetIdentity(turnContext);

        await client.CancelOperationAsync(
            operationId, serviceUrl, identity, null, cancellationToken).ConfigureAwait(false);
    }

    #endregion
}
