// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Teams.Core.Diagnostics;
using Microsoft.Teams.Core.Http;
using Microsoft.Teams.Core.Schema;

namespace Microsoft.Teams.Core;

using CustomHeaders = Dictionary<string, string>;

/// <summary>
/// Provides methods for sending activities to a conversation endpoint using HTTP requests.
/// </summary>
/// <param name="httpClient">The HTTP client instance used to send requests to the conversation service. Must not be null.</param>
/// <param name="logger">The logger instance used for logging. Optional.</param>
public class ConversationClient(HttpClient httpClient, ILogger<ConversationClient> logger = default!)
{
    private readonly BotHttpClient _botHttpClient = new(httpClient, logger);
    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    internal const string ConversationHttpClientName = "BotConversationClient";

    /// <summary>
    /// Gets the underlying <see cref="Http.BotHttpClient"/> used to issue authenticated requests to the conversation service.
    /// Exposed so consumers can reuse the same auth-bound HTTP pipeline for channel- or platform-specific endpoints
    /// not modeled directly on <see cref="ConversationClient"/>.
    /// </summary>
    public virtual BotHttpClient BotHttpClient => _botHttpClient;

    /// <summary>
    /// Sends the specified activity to the conversation endpoint asynchronously using explicit routing.
    /// Use this overload when the activity does not carry its own <c>ServiceUrl</c>/<c>Conversation</c>
    /// (for example content built with a content-only builder).
    /// </summary>
    /// <param name="conversationId">The ID of the conversation. Cannot be null or whitespace.</param>
    /// <param name="activity">The activity to send. Cannot be null.</param>
    /// <param name="serviceUrl">The service URL for the conversation. Cannot be null.</param>
    /// <param name="requestContext">Optional per-request properties (see <see cref="Http.BotRequestContext"/>) used as a fallback; values derived from the activity take precedence.</param>
    /// <param name="customHeaders">Optional custom headers to include in the request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the send operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the response with the ID of the sent activity.</returns>
    public virtual async Task<SendActivityResponse?> SendActivityAsync(string conversationId, CoreActivity activity, Uri serviceUrl, BotRequestContext? requestContext = null, CustomHeaders? customHeaders = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);
        ArgumentNullException.ThrowIfNull(serviceUrl);
        ArgumentException.ThrowIfNullOrWhiteSpace(conversationId);

#pragma warning disable ExperimentalTeamsTargeted
        bool isTargeted = activity.Recipient?.IsTargeted == true;
#pragma warning restore ExperimentalTeamsTargeted

        string url = $"{serviceUrl.ToString().TrimEnd('/')}/v3/conversations/{Uri.EscapeDataString(conversationId)}/activities/";

        if (activity.ChannelId == "agents")
        {
            logger.TruncatingConversationId();
            string convId = "acf"; //conversationId.Length > 100 ? conversationId[..100] : conversationId;
            url = $"{serviceUrl.ToString().TrimEnd('/')}/v3/conversations/{Uri.EscapeDataString(convId)}/activities/";
        }

        if (isTargeted)
        {
            url += url.Contains('?', StringComparison.Ordinal) ? "&isTargetedActivity=true" : "?isTargetedActivity=true";
        }

        string body = activity.ToJson();

        KeyValuePair<string, object?> opTag = new(Telemetry.Tags.Operation, Telemetry.Operations.SendActivity);
        using Activity? span = Telemetry.Source.StartActivity(Telemetry.Spans.ConversationClient, ActivityKind.Client);
        if (span is not null)
        {
            span.SetTag(Telemetry.Tags.Operation, Telemetry.Operations.SendActivity);
            span.SetTag(Telemetry.Tags.ServiceUrl, serviceUrl.ToString());
            span.SetTag(Telemetry.Tags.ConversationId, conversationId);
            span.SetTag(Telemetry.Tags.ActivityType, activity.Type);
        }
        try
        {
            SendActivityResponse? response = await _botHttpClient.SendAsync<SendActivityResponse>(
                HttpMethod.Post,
                url,
                body,
                CreateRequestOptions(requestContext, "sending activity", customHeaders),
                cancellationToken).ConfigureAwait(false);
            span?.SetTag(Telemetry.Tags.ActivityId, response?.Id);
            Telemetry.OutboundCalls.Add(1, opTag);
            return response;
        }
        catch (Exception ex)
        {
            span.RecordException(ex);
            Telemetry.OutboundErrors.Add(1, opTag);
            throw;
        }
    }

    /// <summary>
    /// Updates an existing activity in a conversation.
    /// </summary>
    /// <param name="conversationId">The ID of the conversation. Cannot be null or whitespace.</param>
    /// <param name="activityId">The ID of the activity to update. Cannot be null or whitespace.</param>
    /// <param name="activity">The updated activity data. Cannot be null.</param>
    /// <param name="serviceUrl">The service URL for the conversation. Cannot be null.</param>
    /// <param name="isTargeted">Whether this is a targeted activity visible only to a specific recipient.</param>
    /// <param name="requestContext">Optional per-request properties (see <see cref="Http.BotRequestContext"/>) to stamp onto the request's options.</param>
    /// <param name="customHeaders">Optional custom headers to include in the request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the update operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the response with the ID of the updated activity.</returns>
    /// <exception cref="HttpRequestException">Thrown if the activity could not be updated successfully.</exception>
    public virtual async Task<UpdateActivityResponse> UpdateActivityAsync(string conversationId, string activityId, CoreActivity activity, Uri serviceUrl, bool isTargeted = false, BotRequestContext? requestContext = null, CustomHeaders? customHeaders = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(conversationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(activityId);
        ArgumentNullException.ThrowIfNull(activity);
        ArgumentNullException.ThrowIfNull(serviceUrl);

        string url = $"{serviceUrl.ToString().TrimEnd('/')}/v3/conversations/{Uri.EscapeDataString(conversationId)}/activities/{Uri.EscapeDataString(activityId)}";

        if (isTargeted)
        {
            url += "?isTargetedActivity=true";
        }

        string body = activity.ToJson();

        logger.UpdatingActivity(url, body);

        KeyValuePair<string, object?> opTag = new(Telemetry.Tags.Operation, Telemetry.Operations.UpdateActivity);
        using Activity? span = Telemetry.Source.StartActivity(Telemetry.Spans.ConversationClient, ActivityKind.Client);
        if (span is not null)
        {
            span.SetTag(Telemetry.Tags.Operation, Telemetry.Operations.UpdateActivity);
            span.SetTag(Telemetry.Tags.ServiceUrl, serviceUrl.ToString());
            span.SetTag(Telemetry.Tags.ConversationId, conversationId);
            span.SetTag(Telemetry.Tags.ActivityId, activityId);
            span.SetTag(Telemetry.Tags.ActivityType, activity.Type);
        }
        try
        {
            UpdateActivityResponse response = (await _botHttpClient.SendAsync<UpdateActivityResponse>(
                HttpMethod.Put,
                url,
                body,
                CreateRequestOptions(requestContext, "updating activity", customHeaders),
                cancellationToken).ConfigureAwait(false))!;
            Telemetry.OutboundCalls.Add(1, opTag);
            return response;
        }
        catch (Exception ex)
        {
            span.RecordException(ex);
            Telemetry.OutboundErrors.Add(1, opTag);
            throw;
        }
    }


    /// <summary>
    /// Updates an existing targeted activity in a conversation.
    /// The activity body is sent with the targeted recipient to avoid "Cannot edit Recipient of Targeted Message" errors.
    /// </summary>
    /// <param name="conversationId">The ID of the conversation. Cannot be null or whitespace.</param>
    /// <param name="activityId">The ID of the activity to update. Cannot be null or whitespace.</param>
    /// <param name="activity">The updated activity data. Cannot be null. Must contain a valid ServiceUrl.</param>
    /// <param name="serviceUrl">The service URL for the conversation. Cannot be null.</param>
    /// <param name="requestContext">Optional per-request properties (see <see cref="Http.BotRequestContext"/>) to stamp onto the request's options.</param>
    /// <param name="customHeaders">Optional custom headers to include in the request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the update operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the response with the ID of the updated activity.</returns>
    /// <exception cref="HttpRequestException">Thrown if the activity could not be updated successfully.</exception>
    public virtual async Task<UpdateActivityResponse> UpdateTargetedActivityAsync(string conversationId, string activityId, CoreActivity activity, Uri serviceUrl, BotRequestContext? requestContext = null, CustomHeaders? customHeaders = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(conversationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(activityId);
        ArgumentNullException.ThrowIfNull(activity);
        ArgumentNullException.ThrowIfNull(serviceUrl);

        string url = $"{serviceUrl.ToString().TrimEnd('/')}/v3/conversations/{Uri.EscapeDataString(conversationId)}/activities/{Uri.EscapeDataString(activityId)}?isTargetedActivity=true";

        string body = activity.ToJson();

        logger.UpdatingTargetedActivity(url, body);

        KeyValuePair<string, object?> opTag = new(Telemetry.Tags.Operation, Telemetry.Operations.UpdateActivity);
        using Activity? span = Telemetry.Source.StartActivity(Telemetry.Spans.ConversationClient, ActivityKind.Client);
        if (span is not null)
        {
            span.SetTag(Telemetry.Tags.Operation, Telemetry.Operations.UpdateActivity);
            span.SetTag(Telemetry.Tags.ServiceUrl, serviceUrl.ToString());
            span.SetTag(Telemetry.Tags.ConversationId, conversationId);
            span.SetTag(Telemetry.Tags.ActivityId, activityId);
            span.SetTag(Telemetry.Tags.ActivityType, activity.Type);
        }
        try
        {
            UpdateActivityResponse response = (await _botHttpClient.SendAsync<UpdateActivityResponse>(
                HttpMethod.Put,
                url,
                body,
                CreateRequestOptions(requestContext, "updating targeted activity", customHeaders),
                cancellationToken).ConfigureAwait(false))!;
            Telemetry.OutboundCalls.Add(1, opTag);
            return response;
        }
        catch (Exception ex)
        {
            span.RecordException(ex);
            Telemetry.OutboundErrors.Add(1, opTag);
            throw;
        }
    }

    /// <summary>
    /// Deletes an existing targeted activity from a conversation.
    /// </summary>
    /// <param name="conversationId">The ID of the conversation. Cannot be null or whitespace.</param>
    /// <param name="activityId">The ID of the activity to delete. Cannot be null or whitespace.</param>
    /// <param name="serviceUrl">The service URL for the conversation. Cannot be null.</param>
    /// <param name="requestContext">Optional per-request properties (see <see cref="Http.BotRequestContext"/>) to stamp onto the request's options.</param>
    /// <param name="customHeaders">Optional custom headers to include in the request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the delete operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="HttpRequestException">Thrown if the activity could not be deleted successfully.</exception>
    public virtual Task DeleteTargetedActivityAsync(string conversationId, string activityId, Uri serviceUrl, BotRequestContext? requestContext = null, CustomHeaders? customHeaders = null, CancellationToken cancellationToken = default)
        => DeleteActivityAsync(conversationId, activityId, serviceUrl, isTargeted: true, requestContext, customHeaders, cancellationToken);

    /// <summary>
    /// Deletes an existing activity from a conversation.
    /// </summary>
    /// <param name="conversationId">The ID of the conversation. Cannot be null or whitespace.</param>
    /// <param name="activityId">The ID of the activity to delete. Cannot be null or whitespace.</param>
    /// <param name="serviceUrl">The service URL for the conversation. Cannot be null.</param>
    /// <param name="requestContext">Optional per-request properties (see <see cref="Http.BotRequestContext"/>) to stamp onto the request's options.</param>
    /// <param name="customHeaders">Optional custom headers to include in the request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the delete operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="HttpRequestException">Thrown if the activity could not be deleted successfully.</exception>
    public virtual Task DeleteActivityAsync(string conversationId, string activityId, Uri serviceUrl, BotRequestContext? requestContext = null, CustomHeaders? customHeaders = null, CancellationToken cancellationToken = default)
        => DeleteActivityAsync(conversationId, activityId, serviceUrl, isTargeted: false, requestContext, customHeaders, cancellationToken);

    /// <summary>
    /// Deletes an existing activity from a conversation.
    /// </summary>
    /// <param name="conversationId">The ID of the conversation. Cannot be null or whitespace.</param>
    /// <param name="activityId">The ID of the activity to delete. Cannot be null or whitespace.</param>
    /// <param name="serviceUrl">The service URL for the conversation. Cannot be null.</param>
    /// <param name="isTargeted">If true, deletes a targeted activity.</param>
    /// <param name="requestContext">Optional per-request properties (see <see cref="Http.BotRequestContext"/>) to stamp onto the request's options.</param>
    /// <param name="customHeaders">Optional custom headers to include in the request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the delete operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="HttpRequestException">Thrown if the activity could not be deleted successfully.</exception>
    public virtual async Task DeleteActivityAsync(string conversationId, string activityId, Uri serviceUrl, bool isTargeted, BotRequestContext? requestContext = null, CustomHeaders? customHeaders = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(conversationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(activityId);
        ArgumentNullException.ThrowIfNull(serviceUrl);

        string url = $"{serviceUrl.ToString().TrimEnd('/')}/v3/conversations/{Uri.EscapeDataString(conversationId)}/activities/{Uri.EscapeDataString(activityId)}";

        if (isTargeted)
        {
            url += "?isTargetedActivity=true";
        }

        KeyValuePair<string, object?> opTag = new(Telemetry.Tags.Operation, Telemetry.Operations.DeleteActivity);
        using Activity? span = Telemetry.Source.StartActivity(Telemetry.Spans.ConversationClient, ActivityKind.Client);
        if (span is not null)
        {
            span.SetTag(Telemetry.Tags.Operation, Telemetry.Operations.DeleteActivity);
            span.SetTag(Telemetry.Tags.ServiceUrl, serviceUrl.ToString());
            span.SetTag(Telemetry.Tags.ConversationId, conversationId);
            span.SetTag(Telemetry.Tags.ActivityId, activityId);
        }
        try
        {
            await _botHttpClient.SendAsync(
                HttpMethod.Delete,
                url,
                body: null,
                CreateRequestOptions(requestContext, "deleting activity", customHeaders),
                cancellationToken).ConfigureAwait(false);
            Telemetry.OutboundCalls.Add(1, opTag);
        }
        catch (Exception ex)
        {
            span.RecordException(ex);
            Telemetry.OutboundErrors.Add(1, opTag);
            throw;
        }
    }

    /// <summary>
    /// Deletes an existing activity from a conversation using activity context.
    /// </summary>
    /// <param name="conversationId">The ID of the conversation.</param>
    /// <param name="activity">The activity to delete. Must contain valid Id and ServiceUrl. Cannot be null.</param>
    /// <param name="isTargeted">Whether this is a targeted activity.</param>
    /// <param name="requestContext">Optional per-request properties (see <see cref="Http.BotRequestContext"/>) to stamp onto the request's options.</param>
    /// <param name="customHeaders">Optional custom headers to include in the request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the delete operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="HttpRequestException">Thrown if the activity could not be deleted successfully.</exception>
    public virtual async Task DeleteActivityAsync(string conversationId, CoreActivity activity, bool isTargeted = false, BotRequestContext? requestContext = null, CustomHeaders? customHeaders = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);
        ArgumentException.ThrowIfNullOrWhiteSpace(activity.Id);
        ArgumentException.ThrowIfNullOrWhiteSpace(conversationId);
        ArgumentNullException.ThrowIfNull(activity.ServiceUrl);

        await DeleteActivityAsync(
            conversationId,
            activity.Id,
            activity.ServiceUrl,
            isTargeted,
            requestContext,
            customHeaders,
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the members of a conversation.
    /// </summary>
    /// <param name="conversationId">The ID of the conversation. Cannot be null or whitespace.</param>
    /// <param name="serviceUrl">The service URL for the conversation. Cannot be null.</param>
    /// <param name="requestContext">Optional per-request properties (see <see cref="Http.BotRequestContext"/>) to stamp onto the request's options.</param>
    /// <param name="customHeaders">Optional custom headers to include in the request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of conversation members.</returns>
    /// <exception cref="HttpRequestException">Thrown if the members could not be retrieved successfully.</exception>
    public virtual async Task<IList<ChannelAccount>> GetConversationMembersAsync(string conversationId, Uri serviceUrl, BotRequestContext? requestContext = null, CustomHeaders? customHeaders = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(conversationId);
        ArgumentNullException.ThrowIfNull(serviceUrl);

        string url = $"{serviceUrl.ToString().TrimEnd('/')}/v3/conversations/{Uri.EscapeDataString(conversationId)}/members";

        return (await _botHttpClient.SendAsync<IList<ChannelAccount>>(
            HttpMethod.Get,
            url,
            body: null,
            CreateRequestOptions(requestContext, "getting conversation members", customHeaders),
            cancellationToken).ConfigureAwait(false))!;
    }


    /// <summary>
    /// Gets a specific member of a conversation with strongly-typed result.
    /// </summary>
    /// <typeparam name="T">The type of conversation account to return. Must inherit from <see cref="ChannelAccount"/>.</typeparam>
    /// <param name="conversationId">The ID of the conversation. Cannot be null or whitespace.</param>
    /// <param name="userId">The ID of the user to retrieve. Cannot be null or whitespace.</param>
    /// <param name="serviceUrl">The service URL for the conversation. Cannot be null.</param>
    /// <param name="requestContext">Optional per-request properties (see <see cref="Http.BotRequestContext"/>) to stamp onto the request's options.</param>
    /// <param name="customHeaders">Optional custom headers to include in the request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the conversation member
    /// of type T with detailed information about the user.
    /// </returns>
    /// <exception cref="HttpRequestException">Thrown if the member could not be retrieved successfully.</exception>
    public virtual async Task<T> GetConversationMemberAsync<T>(string conversationId, string userId, Uri serviceUrl, BotRequestContext? requestContext = null, CustomHeaders? customHeaders = null, CancellationToken cancellationToken = default) where T : ChannelAccount
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(conversationId);
        ArgumentNullException.ThrowIfNull(serviceUrl);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        string url = $"{serviceUrl.ToString().TrimEnd('/')}/v3/conversations/{Uri.EscapeDataString(conversationId)}/members/{Uri.EscapeDataString(userId)}";

        return (await _botHttpClient.SendAsync<T>(
            HttpMethod.Get,
            url,
            body: null,
            CreateRequestOptions(requestContext, "getting conversation member", customHeaders),
            cancellationToken).ConfigureAwait(false))!;
    }

    /// <summary>
    /// Gets the conversations in which the bot has participated.
    /// </summary>
    /// <param name="serviceUrl">The service URL for the bot. Cannot be null.</param>
    /// <param name="continuationToken">Optional continuation token for pagination.</param>
    /// <param name="requestContext">Optional per-request properties (see <see cref="Http.BotRequestContext"/>) to stamp onto the request's options.</param>
    /// <param name="customHeaders">Optional custom headers to include in the request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the conversations and an optional continuation token.</returns>
    /// <exception cref="HttpRequestException">Thrown if the conversations could not be retrieved successfully.</exception>
    public virtual async Task<GetConversationsResponse> GetConversationsAsync(Uri serviceUrl, string? continuationToken = null, BotRequestContext? requestContext = null, CustomHeaders? customHeaders = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(serviceUrl);

        string url = $"{serviceUrl.ToString().TrimEnd('/')}/v3/conversations";
        if (!string.IsNullOrWhiteSpace(continuationToken))
        {
            url += $"?continuationToken={Uri.EscapeDataString(continuationToken)}";
        }

        return (await _botHttpClient.SendAsync<GetConversationsResponse>(
            HttpMethod.Get,
            url,
            body: null,
            CreateRequestOptions(requestContext, "getting conversations", customHeaders),
            cancellationToken).ConfigureAwait(false))!;
    }

    /// <summary>
    /// Gets the members of a specific activity.
    /// </summary>
    /// <param name="conversationId">The ID of the conversation. Cannot be null or whitespace.</param>
    /// <param name="activityId">The ID of the activity. Cannot be null or whitespace.</param>
    /// <param name="serviceUrl">The service URL for the conversation. Cannot be null.</param>
    /// <param name="requestContext">Optional per-request properties (see <see cref="Http.BotRequestContext"/>) to stamp onto the request's options.</param>
    /// <param name="customHeaders">Optional custom headers to include in the request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of members for the activity.</returns>
    /// <exception cref="HttpRequestException">Thrown if the activity members could not be retrieved successfully.</exception>
    public virtual async Task<IList<ChannelAccount>> GetActivityMembersAsync(string conversationId, string activityId, Uri serviceUrl, BotRequestContext? requestContext = null, CustomHeaders? customHeaders = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(conversationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(activityId);
        ArgumentNullException.ThrowIfNull(serviceUrl);

        string url = $"{serviceUrl.ToString().TrimEnd('/')}/v3/conversations/{Uri.EscapeDataString(conversationId)}/activities/{Uri.EscapeDataString(activityId)}/members";

        return (await _botHttpClient.SendAsync<IList<ChannelAccount>>(
            HttpMethod.Get,
            url,
            body: null,
            CreateRequestOptions(requestContext, "getting activity members", customHeaders),
            cancellationToken).ConfigureAwait(false))!;
    }

    /// <summary>
    /// Creates a new conversation.
    /// </summary>
    /// <param name="parameters">The parameters for creating the conversation. Cannot be null.</param>
    /// <param name="serviceUrl">The service URL for the bot. Cannot be null.</param>
    /// <param name="requestContext">Optional per-request properties (see <see cref="Http.BotRequestContext"/>) to stamp onto the request's options.</param>
    /// <param name="customHeaders">Optional custom headers to include in the request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the conversation resource response with the conversation ID.</returns>
    /// <exception cref="HttpRequestException">Thrown if the conversation could not be created successfully.</exception>
    public virtual async Task<CreateConversationResponse> CreateConversationAsync(ConversationParameters parameters, Uri serviceUrl, BotRequestContext? requestContext = null, CustomHeaders? customHeaders = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        ArgumentNullException.ThrowIfNull(serviceUrl);

        BotRequestContext? properties = BotRequestContext.Merge(requestContext, BotRequestContext.FromActivity(parameters.Activity));

        string url = $"{serviceUrl.ToString().TrimEnd('/')}/v3/conversations";

        string paramsJson = JsonSerializer.Serialize(parameters, _jsonSerializerOptions);

        logger.CreatingConversation(url, paramsJson);

        return (await _botHttpClient.SendAsync<CreateConversationResponse>(
            HttpMethod.Post,
            url,
            paramsJson,
            CreateRequestOptions(properties, "creating conversation", customHeaders),
            cancellationToken).ConfigureAwait(false))!;
    }

    /// <summary>
    /// Gets the members of a conversation one page at a time.
    /// </summary>
    /// <param name="conversationId">The ID of the conversation. Cannot be null or whitespace.</param>
    /// <param name="serviceUrl">The service URL for the conversation. Cannot be null.</param>
    /// <param name="pageSize">Optional page size for the number of members to retrieve.</param>
    /// <param name="continuationToken">Optional continuation token for pagination.</param>
    /// <param name="requestContext">Optional per-request properties (see <see cref="Http.BotRequestContext"/>) to stamp onto the request's options.</param>
    /// <param name="customHeaders">Optional custom headers to include in the request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a page of members and an optional continuation token.</returns>
    /// <exception cref="HttpRequestException">Thrown if the conversation members could not be retrieved successfully.</exception>
    public virtual async Task<PagedMembersResult> GetConversationPagedMembersAsync(string conversationId, Uri serviceUrl, int? pageSize = null, string? continuationToken = null, BotRequestContext? requestContext = null, CustomHeaders? customHeaders = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(conversationId);
        ArgumentNullException.ThrowIfNull(serviceUrl);

        string url = $"{serviceUrl.ToString().TrimEnd('/')}/v3/conversations/{Uri.EscapeDataString(conversationId)}/pagedmembers";

        List<string> queryParams = [];
        if (pageSize.HasValue)
        {
            queryParams.Add($"pageSize={pageSize.Value}");
        }
        if (!string.IsNullOrWhiteSpace(continuationToken))
        {
            queryParams.Add($"continuationToken={Uri.EscapeDataString(continuationToken)}");
        }
        if (queryParams.Count > 0)
        {
            url += $"?{string.Join("&", queryParams)}";
        }

        return (await _botHttpClient.SendAsync<PagedMembersResult>(
            HttpMethod.Get,
            url,
            body: null,
            CreateRequestOptions(requestContext, "getting paged conversation members", customHeaders),
            cancellationToken).ConfigureAwait(false))!;
    }

    /// <summary>
    /// Deletes a member from a conversation.
    /// </summary>
    /// <param name="conversationId">The ID of the conversation. Cannot be null or whitespace.</param>
    /// <param name="memberId">The ID of the member to delete. Cannot be null or whitespace.</param>
    /// <param name="serviceUrl">The service URL for the conversation. Cannot be null.</param>
    /// <param name="requestContext">Optional per-request properties (see <see cref="Http.BotRequestContext"/>) to stamp onto the request's options.</param>
    /// <param name="customHeaders">Optional custom headers to include in the request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="HttpRequestException">Thrown if the member could not be deleted successfully.</exception>
    /// <remarks>If the deleted member was the last member of the conversation, the conversation is also deleted.</remarks>
    public virtual async Task DeleteConversationMemberAsync(string conversationId, string memberId, Uri serviceUrl, BotRequestContext? requestContext = null, CustomHeaders? customHeaders = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(conversationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(memberId);
        ArgumentNullException.ThrowIfNull(serviceUrl);

        string url = $"{serviceUrl.ToString().TrimEnd('/')}/v3/conversations/{Uri.EscapeDataString(conversationId)}/members/{Uri.EscapeDataString(memberId)}";

        await _botHttpClient.SendAsync(
            HttpMethod.Delete,
            url,
            body: null,
            CreateRequestOptions(requestContext, "deleting conversation member", customHeaders),
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Uploads and sends historic activities to the conversation.
    /// </summary>
    /// <param name="conversationId">The ID of the conversation. Cannot be null or whitespace.</param>
    /// <param name="transcript">The transcript containing the historic activities. Cannot be null.</param>
    /// <param name="serviceUrl">The service URL for the conversation. Cannot be null.</param>
    /// <param name="requestContext">Optional per-request properties (see <see cref="Http.BotRequestContext"/>) to stamp onto the request's options.</param>
    /// <param name="customHeaders">Optional custom headers to include in the request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the response with a resource ID.</returns>
    /// <exception cref="HttpRequestException">Thrown if the history could not be sent successfully.</exception>
    /// <remarks>Activities in the transcript must have unique IDs and appropriate timestamps for proper rendering.</remarks>
    public virtual async Task<SendConversationHistoryResponse> SendConversationHistoryAsync(string conversationId, Transcript transcript, Uri serviceUrl, BotRequestContext? requestContext = null, CustomHeaders? customHeaders = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(conversationId);
        ArgumentNullException.ThrowIfNull(transcript);
        ArgumentNullException.ThrowIfNull(serviceUrl);

        string url = $"{serviceUrl.ToString().TrimEnd('/')}/v3/conversations/{Uri.EscapeDataString(conversationId)}/activities/history";

        string transcriptJson = JsonSerializer.Serialize(transcript, _jsonSerializerOptions);
        logger.SendingConversationHistory(url, transcriptJson);

        return (await _botHttpClient.SendAsync<SendConversationHistoryResponse>(
            HttpMethod.Post,
            url,
            transcriptJson,
            CreateRequestOptions(requestContext, "sending conversation history", customHeaders),
            cancellationToken).ConfigureAwait(false))!;
    }

    /// <summary>
    /// Uploads an attachment to the channel's blob storage.
    /// </summary>
    /// <param name="conversationId">The ID of the conversation. Cannot be null or whitespace.</param>
    /// <param name="attachmentData">The attachment data to upload. Cannot be null.</param>
    /// <param name="serviceUrl">The service URL for the conversation. Cannot be null.</param>
    /// <param name="requestContext">Optional per-request properties (see <see cref="Http.BotRequestContext"/>) to stamp onto the request's options.</param>
    /// <param name="customHeaders">Optional custom headers to include in the request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the response with an attachment ID.</returns>
    /// <exception cref="HttpRequestException">Thrown if the attachment could not be uploaded successfully.</exception>
    /// <remarks>This is useful for storing data in a compliant store when dealing with enterprises.</remarks>
    public virtual async Task<UploadAttachmentResponse> UploadAttachmentAsync(string conversationId, AttachmentData attachmentData, Uri serviceUrl, BotRequestContext? requestContext = null, CustomHeaders? customHeaders = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(conversationId);
        ArgumentNullException.ThrowIfNull(attachmentData);
        ArgumentNullException.ThrowIfNull(serviceUrl);

        string url = $"{serviceUrl.ToString().TrimEnd('/')}/v3/conversations/{Uri.EscapeDataString(conversationId)}/attachments";

        string attachmentDataJson = JsonSerializer.Serialize(attachmentData, _jsonSerializerOptions);
        logger.UploadingAttachment(url, attachmentDataJson);

        return (await _botHttpClient.SendAsync<UploadAttachmentResponse>(
            HttpMethod.Post,
            url,
            attachmentDataJson,
            CreateRequestOptions(requestContext, "uploading attachment", customHeaders),
            cancellationToken).ConfigureAwait(false))!;
    }

    /// <summary>
    /// Adds a reaction to an activity in a conversation.
    /// </summary>
    /// <param name="conversationId">The ID of the conversation. Cannot be null or whitespace.</param>
    /// <param name="activityId">The ID of the activity to react to. Cannot be null or whitespace.</param>
    /// <param name="reactionType">The type of reaction to add (e.g., "like", "heart", "laugh"). Cannot be null or whitespace.</param>
    /// <param name="serviceUrl">The service URL for the conversation. Cannot be null.</param>
    /// <param name="requestContext">Optional per-request properties (see <see cref="Http.BotRequestContext"/>) to stamp onto the request's options.</param>
    /// <param name="customHeaders">Optional custom headers to include in the request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="HttpRequestException">Thrown if the reaction could not be added successfully.</exception>
    public virtual async Task AddReactionAsync(string conversationId, string activityId, string reactionType, Uri serviceUrl, BotRequestContext? requestContext = null, CustomHeaders? customHeaders = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(conversationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(activityId);
        ArgumentException.ThrowIfNullOrWhiteSpace(reactionType);
        ArgumentNullException.ThrowIfNull(serviceUrl);

        string url = $"{serviceUrl.ToString().TrimEnd('/')}/v3/conversations/{Uri.EscapeDataString(conversationId)}/activities/{Uri.EscapeDataString(activityId)}/reactions/{Uri.EscapeDataString(reactionType)}";

        await _botHttpClient.SendAsync(
            HttpMethod.Put,
            url,
            body: null,
            CreateRequestOptions(requestContext, "adding reaction", customHeaders),
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Removes a reaction from an activity in a conversation.
    /// </summary>
    /// <param name="conversationId">The ID of the conversation. Cannot be null or whitespace.</param>
    /// <param name="activityId">The ID of the activity to remove the reaction from. Cannot be null or whitespace.</param>
    /// <param name="reactionType">The type of reaction to remove (e.g., "like", "heart", "laugh"). Cannot be null or whitespace.</param>
    /// <param name="serviceUrl">The service URL for the conversation. Cannot be null.</param>
    /// <param name="requestContext">Optional per-request properties (see <see cref="Http.BotRequestContext"/>) to stamp onto the request's options.</param>
    /// <param name="customHeaders">Optional custom headers to include in the request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="HttpRequestException">Thrown if the reaction could not be removed successfully.</exception>
    public virtual async Task DeleteReactionAsync(string conversationId, string activityId, string reactionType, Uri serviceUrl, BotRequestContext? requestContext = null, CustomHeaders? customHeaders = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(conversationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(activityId);
        ArgumentException.ThrowIfNullOrWhiteSpace(reactionType);
        ArgumentNullException.ThrowIfNull(serviceUrl);

        string url = $"{serviceUrl.ToString().TrimEnd('/')}/v3/conversations/{Uri.EscapeDataString(conversationId)}/activities/{Uri.EscapeDataString(activityId)}/reactions/{Uri.EscapeDataString(reactionType)}";

        await _botHttpClient.SendAsync(
            HttpMethod.Delete,
            url,
            body: null,
            CreateRequestOptions(requestContext, "deleting reaction", customHeaders),
            cancellationToken).ConfigureAwait(false);
    }

    private static BotRequestOptions CreateRequestOptions(BotRequestContext? requestContext, string operationDescription, CustomHeaders? customHeaders) =>
        new()
        {
            RequestContext = requestContext,
            OperationDescription = operationDescription,
            CustomHeaders = customHeaders
        };
}
