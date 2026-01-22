// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Teams.Bot.Core.Http;
using Microsoft.Teams.Bot.Core.Schema;

namespace Microsoft.Teams.Bot.Core;

using CustomHeaders = Dictionary<string, string>;

/// <summary>
/// Provides methods for sending activities to a conversation endpoint using HTTP requests.
/// </summary>
/// <param name="httpClient">The HTTP client instance used to send requests to the conversation service. Must not be null.</param>
/// <param name="logger">The logger instance used for logging. Optional.</param>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1848:Use the LoggerMessage delegates", Justification = "<Pending>")]
public class ConversationClient(HttpClient httpClient, ILogger<ConversationClient> logger = default!)
{
    private readonly BotHttpClient _botHttpClient = new(httpClient, logger);
    internal const string ConversationHttpClientName = "BotConversationClient";

    /// <summary>
    /// Gets the default custom headers that will be included in all requests.
    /// </summary>
    public CustomHeaders DefaultCustomHeaders { get; } = [];

    /// <summary>
    /// Sends the specified activity to the conversation endpoint asynchronously.
    /// </summary>
    /// <param name="activity">The activity to send. Cannot be null. The activity must contain valid conversation and service URL information.</param>
    /// <param name="customHeaders">Optional custom headers to include in the request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the send operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the response with the ID of the sent activity.</returns>
    /// <exception cref="Exception">Thrown if the activity could not be sent successfully. The exception message includes the HTTP status code and
    /// response content.</exception>
    public async Task<SendActivityResponse> SendActivityAsync(CoreActivity activity, CustomHeaders? customHeaders = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);
        ArgumentNullException.ThrowIfNull(activity.Conversation);
        ArgumentException.ThrowIfNullOrWhiteSpace(activity.Conversation.Id);
        ArgumentNullException.ThrowIfNull(activity.ServiceUrl);

        string url = $"{activity.ServiceUrl.ToString().TrimEnd('/')}/v3/conversations/{activity.Conversation.Id}/activities/";

        if (activity.ChannelId == "agents")
        {
            logger.LogInformation("Truncating conversation ID for 'agents' channel to comply with length restrictions.");
            string conversationId = activity.Conversation.Id;
            string convId = conversationId.Length > 325 ? conversationId[..325] : conversationId;
            url = $"{activity.ServiceUrl.ToString().TrimEnd('/')}/v3/conversations/{convId}/activities";
        }

        string body = activity.ToJson();

        logger?.LogTrace("Sending activity to {Url}: {Activity}", url, body);

        return (await _botHttpClient.SendAsync<SendActivityResponse>(
            HttpMethod.Post,
            url,
            body,
            CreateRequestOptions(activity.From.GetAgenticIdentity(), "sending activity", customHeaders),
            cancellationToken).ConfigureAwait(false))!;
    }

    /// <summary>
    /// Updates an existing activity in a conversation.
    /// </summary>
    /// <param name="conversationId">The ID of the conversation. Cannot be null or whitespace.</param>
    /// <param name="activityId">The ID of the activity to update. Cannot be null or whitespace.</param>
    /// <param name="activity">The updated activity data. Cannot be null.</param>
    /// <param name="customHeaders">Optional custom headers to include in the request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the update operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the response with the ID of the updated activity.</returns>
    /// <exception cref="HttpRequestException">Thrown if the activity could not be updated successfully.</exception>
    public async Task<UpdateActivityResponse> UpdateActivityAsync(string conversationId, string activityId, CoreActivity activity, CustomHeaders? customHeaders = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(conversationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(activityId);
        ArgumentNullException.ThrowIfNull(activity);
        ArgumentNullException.ThrowIfNull(activity.ServiceUrl);

        string url = $"{activity.ServiceUrl.ToString().TrimEnd('/')}/v3/conversations/{conversationId}/activities/{activityId}";
        string body = activity.ToJson();

        logger.LogTrace("Updating activity at {Url}: {Activity}", url, body);

        return (await _botHttpClient.SendAsync<UpdateActivityResponse>(
            HttpMethod.Put,
            url,
            body,
            CreateRequestOptions(activity.From.GetAgenticIdentity(), "updating activity", customHeaders),
            cancellationToken).ConfigureAwait(false))!;
    }


    /// <summary>
    /// Deletes an existing activity from a conversation.
    /// </summary>
    /// <param name="conversationId">The ID of the conversation. Cannot be null or whitespace.</param>
    /// <param name="activityId">The ID of the activity to delete. Cannot be null or whitespace.</param>
    /// <param name="serviceUrl">The service URL for the conversation. Cannot be null.</param>
    /// <param name="agenticIdentity">Optional agentic identity for authentication.</param>
    /// <param name="customHeaders">Optional custom headers to include in the request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the delete operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="HttpRequestException">Thrown if the activity could not be deleted successfully.</exception>
    public async Task DeleteActivityAsync(string conversationId, string activityId, Uri serviceUrl, AgenticIdentity? agenticIdentity = null, CustomHeaders? customHeaders = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(conversationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(activityId);
        ArgumentNullException.ThrowIfNull(serviceUrl);

        string url = $"{serviceUrl.ToString().TrimEnd('/')}/v3/conversations/{conversationId}/activities/{activityId}";

        logger.LogTrace("Deleting activity at {Url}", url);

        await _botHttpClient.SendAsync(
            HttpMethod.Delete,
            url,
            body: null,
            CreateRequestOptions(agenticIdentity, "deleting activity", customHeaders),
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Deletes an existing activity from a conversation using activity context.
    /// </summary>
    /// <param name="activity">The activity to delete. Must contain valid Id, Conversation.Id, and ServiceUrl. Cannot be null.</param>
    /// <param name="customHeaders">Optional custom headers to include in the request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the delete operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="HttpRequestException">Thrown if the activity could not be deleted successfully.</exception>
    public async Task DeleteActivityAsync(CoreActivity activity, CustomHeaders? customHeaders = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);
        ArgumentException.ThrowIfNullOrWhiteSpace(activity.Id);
        ArgumentNullException.ThrowIfNull(activity.Conversation);
        ArgumentException.ThrowIfNullOrWhiteSpace(activity.Conversation.Id);
        ArgumentNullException.ThrowIfNull(activity.ServiceUrl);

        await DeleteActivityAsync(
            activity.Conversation.Id,
            activity.Id,
            activity.ServiceUrl,
            activity.From.GetAgenticIdentity(),
            customHeaders,
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the members of a conversation.
    /// </summary>
    /// <param name="conversationId">The ID of the conversation. Cannot be null or whitespace.</param>
    /// <param name="serviceUrl">The service URL for the conversation. Cannot be null.</param>
    /// <param name="agenticIdentity">Optional agentic identity for authentication.</param>
    /// <param name="customHeaders">Optional custom headers to include in the request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of conversation members.</returns>
    /// <exception cref="HttpRequestException">Thrown if the members could not be retrieved successfully.</exception>
    public async Task<IList<ConversationAccount>> GetConversationMembersAsync(string conversationId, Uri serviceUrl, AgenticIdentity? agenticIdentity = null, CustomHeaders? customHeaders = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(conversationId);
        ArgumentNullException.ThrowIfNull(serviceUrl);

        string url = $"{serviceUrl.ToString().TrimEnd('/')}/v3/conversations/{conversationId}/members";

        logger.LogTrace("Getting conversation members from {Url}", url);

        return (await _botHttpClient.SendAsync<IList<ConversationAccount>>(
            HttpMethod.Get,
            url,
            body: null,
            CreateRequestOptions(agenticIdentity, "getting conversation members", customHeaders),
            cancellationToken).ConfigureAwait(false))!;
    }


    /// <summary>
    /// Gets a specific member of a conversation with strongly-typed result.
    /// </summary>
    /// <typeparam name="T">The type of conversation account to return. Must inherit from <see cref="ConversationAccount"/>.</typeparam>
    /// <param name="conversationId">The ID of the conversation. Cannot be null or whitespace.</param>
    /// <param name="userId">The ID of the user to retrieve. Cannot be null or whitespace.</param>
    /// <param name="serviceUrl">The service URL for the conversation. Cannot be null.</param>
    /// <param name="agenticIdentity">Optional agentic identity for authentication.</param>
    /// <param name="customHeaders">Optional custom headers to include in the request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the conversation member
    /// of type T with detailed information about the user.
    /// </returns>
    /// <exception cref="HttpRequestException">Thrown if the member could not be retrieved successfully.</exception>
    public async Task<T> GetConversationMemberAsync<T>(string conversationId, string userId, Uri serviceUrl, AgenticIdentity? agenticIdentity = null, CustomHeaders? customHeaders = null, CancellationToken cancellationToken = default) where T : ConversationAccount
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(conversationId);
        ArgumentNullException.ThrowIfNull(serviceUrl);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        string url = $"{serviceUrl.ToString().TrimEnd('/')}/v3/conversations/{conversationId}/members/{userId}";

        logger.LogTrace("Getting conversation members from {Url}", url);

        return (await _botHttpClient.SendAsync<T>(
            HttpMethod.Get,
            url,
            body: null,
            CreateRequestOptions(agenticIdentity, "getting conversation member", customHeaders),
            cancellationToken).ConfigureAwait(false))!;
    }

    /// <summary>
    /// Gets the conversations in which the bot has participated.
    /// </summary>
    /// <param name="serviceUrl">The service URL for the bot. Cannot be null.</param>
    /// <param name="continuationToken">Optional continuation token for pagination.</param>
    /// <param name="agenticIdentity">Optional agentic identity for authentication.</param>
    /// <param name="customHeaders">Optional custom headers to include in the request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the conversations and an optional continuation token.</returns>
    /// <exception cref="HttpRequestException">Thrown if the conversations could not be retrieved successfully.</exception>
    public async Task<GetConversationsResponse> GetConversationsAsync(Uri serviceUrl, string? continuationToken = null, AgenticIdentity? agenticIdentity = null, CustomHeaders? customHeaders = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(serviceUrl);

        string url = $"{serviceUrl.ToString().TrimEnd('/')}/v3/conversations";
        if (!string.IsNullOrWhiteSpace(continuationToken))
        {
            url += $"?continuationToken={Uri.EscapeDataString(continuationToken)}";
        }

        logger.LogTrace("Getting conversations from {Url}", url);

        return (await _botHttpClient.SendAsync<GetConversationsResponse>(
            HttpMethod.Get,
            url,
            body: null,
            CreateRequestOptions(agenticIdentity, "getting conversations", customHeaders),
            cancellationToken).ConfigureAwait(false))!;
    }

    /// <summary>
    /// Gets the members of a specific activity.
    /// </summary>
    /// <param name="conversationId">The ID of the conversation. Cannot be null or whitespace.</param>
    /// <param name="activityId">The ID of the activity. Cannot be null or whitespace.</param>
    /// <param name="serviceUrl">The service URL for the conversation. Cannot be null.</param>
    /// <param name="agenticIdentity">Optional agentic identity for authentication.</param>
    /// <param name="customHeaders">Optional custom headers to include in the request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of members for the activity.</returns>
    /// <exception cref="HttpRequestException">Thrown if the activity members could not be retrieved successfully.</exception>
    public async Task<IList<ConversationAccount>> GetActivityMembersAsync(string conversationId, string activityId, Uri serviceUrl, AgenticIdentity? agenticIdentity = null, CustomHeaders? customHeaders = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(conversationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(activityId);
        ArgumentNullException.ThrowIfNull(serviceUrl);

        string url = $"{serviceUrl.ToString().TrimEnd('/')}/v3/conversations/{conversationId}/activities/{activityId}/members";

        logger.LogTrace("Getting activity members from {Url}", url);

        return (await _botHttpClient.SendAsync<IList<ConversationAccount>>(
            HttpMethod.Get,
            url,
            body: null,
            CreateRequestOptions(agenticIdentity, "getting activity members", customHeaders),
            cancellationToken).ConfigureAwait(false))!;
    }

    /// <summary>
    /// Creates a new conversation.
    /// </summary>
    /// <param name="parameters">The parameters for creating the conversation. Cannot be null.</param>
    /// <param name="serviceUrl">The service URL for the bot. Cannot be null.</param>
    /// <param name="agenticIdentity">Optional agentic identity for authentication.</param>
    /// <param name="customHeaders">Optional custom headers to include in the request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the conversation resource response with the conversation ID.</returns>
    /// <exception cref="HttpRequestException">Thrown if the conversation could not be created successfully.</exception>
    public async Task<CreateConversationResponse> CreateConversationAsync(ConversationParameters parameters, Uri serviceUrl, AgenticIdentity? agenticIdentity = null, CustomHeaders? customHeaders = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        ArgumentNullException.ThrowIfNull(serviceUrl);

        string url = $"{serviceUrl.ToString().TrimEnd('/')}/v3/conversations";

        logger.LogTrace("Creating conversation at {Url} with parameters: {Parameters}", url, JsonSerializer.Serialize(parameters));

        return (await _botHttpClient.SendAsync<CreateConversationResponse>(
            HttpMethod.Post,
            url,
            JsonSerializer.Serialize(parameters),
            CreateRequestOptions(agenticIdentity, "creating conversation", customHeaders),
            cancellationToken).ConfigureAwait(false))!;
    }

    /// <summary>
    /// Gets the members of a conversation one page at a time.
    /// </summary>
    /// <param name="conversationId">The ID of the conversation. Cannot be null or whitespace.</param>
    /// <param name="serviceUrl">The service URL for the conversation. Cannot be null.</param>
    /// <param name="pageSize">Optional page size for the number of members to retrieve.</param>
    /// <param name="continuationToken">Optional continuation token for pagination.</param>
    /// <param name="agenticIdentity">Optional agentic identity for authentication.</param>
    /// <param name="customHeaders">Optional custom headers to include in the request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a page of members and an optional continuation token.</returns>
    /// <exception cref="HttpRequestException">Thrown if the conversation members could not be retrieved successfully.</exception>
    public async Task<PagedMembersResult> GetConversationPagedMembersAsync(string conversationId, Uri serviceUrl, int? pageSize = null, string? continuationToken = null, AgenticIdentity? agenticIdentity = null, CustomHeaders? customHeaders = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(conversationId);
        ArgumentNullException.ThrowIfNull(serviceUrl);

        string url = $"{serviceUrl.ToString().TrimEnd('/')}/v3/conversations/{conversationId}/pagedmembers";

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

        logger.LogTrace("Getting paged conversation members from {Url}", url);

        return (await _botHttpClient.SendAsync<PagedMembersResult>(
            HttpMethod.Get,
            url,
            body: null,
            CreateRequestOptions(agenticIdentity, "getting paged conversation members", customHeaders),
            cancellationToken).ConfigureAwait(false))!;
    }

    /// <summary>
    /// Deletes a member from a conversation.
    /// </summary>
    /// <param name="conversationId">The ID of the conversation. Cannot be null or whitespace.</param>
    /// <param name="memberId">The ID of the member to delete. Cannot be null or whitespace.</param>
    /// <param name="serviceUrl">The service URL for the conversation. Cannot be null.</param>
    /// <param name="agenticIdentity">Optional agentic identity for authentication.</param>
    /// <param name="customHeaders">Optional custom headers to include in the request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="HttpRequestException">Thrown if the member could not be deleted successfully.</exception>
    /// <remarks>If the deleted member was the last member of the conversation, the conversation is also deleted.</remarks>
    public async Task DeleteConversationMemberAsync(string conversationId, string memberId, Uri serviceUrl, AgenticIdentity? agenticIdentity = null, CustomHeaders? customHeaders = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(conversationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(memberId);
        ArgumentNullException.ThrowIfNull(serviceUrl);

        string url = $"{serviceUrl.ToString().TrimEnd('/')}/v3/conversations/{conversationId}/members/{memberId}";

        logger.LogTrace("Deleting conversation member at {Url}", url);

        await _botHttpClient.SendAsync(
            HttpMethod.Delete,
            url,
            body: null,
            CreateRequestOptions(agenticIdentity, "deleting conversation member", customHeaders),
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Uploads and sends historic activities to the conversation.
    /// </summary>
    /// <param name="conversationId">The ID of the conversation. Cannot be null or whitespace.</param>
    /// <param name="transcript">The transcript containing the historic activities. Cannot be null.</param>
    /// <param name="serviceUrl">The service URL for the conversation. Cannot be null.</param>
    /// <param name="agenticIdentity">Optional agentic identity for authentication.</param>
    /// <param name="customHeaders">Optional custom headers to include in the request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the response with a resource ID.</returns>
    /// <exception cref="HttpRequestException">Thrown if the history could not be sent successfully.</exception>
    /// <remarks>Activities in the transcript must have unique IDs and appropriate timestamps for proper rendering.</remarks>
    public async Task<SendConversationHistoryResponse> SendConversationHistoryAsync(string conversationId, Transcript transcript, Uri serviceUrl, AgenticIdentity? agenticIdentity = null, CustomHeaders? customHeaders = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(conversationId);
        ArgumentNullException.ThrowIfNull(transcript);
        ArgumentNullException.ThrowIfNull(serviceUrl);

        string url = $"{serviceUrl.ToString().TrimEnd('/')}/v3/conversations/{conversationId}/activities/history";

        logger.LogTrace("Sending conversation history to {Url}: {Transcript}", url, JsonSerializer.Serialize(transcript));

        return (await _botHttpClient.SendAsync<SendConversationHistoryResponse>(
            HttpMethod.Post,
            url,
            JsonSerializer.Serialize(transcript),
            CreateRequestOptions(agenticIdentity, "sending conversation history", customHeaders),
            cancellationToken).ConfigureAwait(false))!;
    }

    /// <summary>
    /// Uploads an attachment to the channel's blob storage.
    /// </summary>
    /// <param name="conversationId">The ID of the conversation. Cannot be null or whitespace.</param>
    /// <param name="attachmentData">The attachment data to upload. Cannot be null.</param>
    /// <param name="serviceUrl">The service URL for the conversation. Cannot be null.</param>
    /// <param name="agenticIdentity">Optional agentic identity for authentication.</param>
    /// <param name="customHeaders">Optional custom headers to include in the request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the response with an attachment ID.</returns>
    /// <exception cref="HttpRequestException">Thrown if the attachment could not be uploaded successfully.</exception>
    /// <remarks>This is useful for storing data in a compliant store when dealing with enterprises.</remarks>
    public async Task<UploadAttachmentResponse> UploadAttachmentAsync(string conversationId, AttachmentData attachmentData, Uri serviceUrl, AgenticIdentity? agenticIdentity = null, CustomHeaders? customHeaders = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(conversationId);
        ArgumentNullException.ThrowIfNull(attachmentData);
        ArgumentNullException.ThrowIfNull(serviceUrl);

        string url = $"{serviceUrl.ToString().TrimEnd('/')}/v3/conversations/{conversationId}/attachments";

        logger.LogTrace("Uploading attachment to {Url}: {AttachmentData}", url, JsonSerializer.Serialize(attachmentData));

        return (await _botHttpClient.SendAsync<UploadAttachmentResponse>(
            HttpMethod.Post,
            url,
            JsonSerializer.Serialize(attachmentData),
            CreateRequestOptions(agenticIdentity, "uploading attachment", customHeaders),
            cancellationToken).ConfigureAwait(false))!;
    }

    private BotRequestOptions CreateRequestOptions(AgenticIdentity? agenticIdentity, string operationDescription, CustomHeaders? customHeaders) =>
        new()
        {
            AgenticIdentity = agenticIdentity,
            OperationDescription = operationDescription,
            DefaultHeaders = DefaultCustomHeaders,
            CustomHeaders = customHeaders
        };
}
