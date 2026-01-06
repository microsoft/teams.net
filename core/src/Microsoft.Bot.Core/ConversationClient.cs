// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net.Mime;
using System.Text;
using System.Text.Json;
using Microsoft.Bot.Core.Hosting;
using Microsoft.Bot.Core.Schema;
using Microsoft.Extensions.Logging;

namespace Microsoft.Bot.Core;

using CustomHeaders = Dictionary<string, string>;

/// <summary>
/// Provides methods for sending activities to a conversation endpoint using HTTP requests.
/// </summary>
/// <param name="httpClient">The HTTP client instance used to send requests to the conversation service. Must not be null.</param>
/// <param name="logger">The logger instance used for logging. Optional.</param>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1848:Use the LoggerMessage delegates", Justification = "<Pending>")]
public class ConversationClient(HttpClient httpClient, ILogger<ConversationClient> logger = default!)
{
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
        ArgumentNullException.ThrowIfNullOrWhiteSpace(activity.Conversation.Id);
        ArgumentNullException.ThrowIfNull(activity.ServiceUrl);

        if (activity.Type == "invokeResponse")
        {
            return new SendActivityResponse();
        }

        string url = $"{activity.ServiceUrl.ToString().TrimEnd('/')}/v3/conversations/{activity.Conversation.Id}/activities/";
        string body = activity.ToJson();

        logger?.LogTrace("Sending activity to {Url}: {Activity}", url, body);

        return await SendHttpRequestAsync<SendActivityResponse>(
            HttpMethod.Post,
            url,
            body,
            activity.From.GetAgenticIdentity(),
            "sending activity",
            customHeaders,
            cancellationToken).ConfigureAwait(false);
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
        ArgumentNullException.ThrowIfNullOrWhiteSpace(conversationId);
        ArgumentNullException.ThrowIfNullOrWhiteSpace(activityId);
        ArgumentNullException.ThrowIfNull(activity);
        ArgumentNullException.ThrowIfNull(activity.ServiceUrl);

        string url = $"{activity.ServiceUrl.ToString().TrimEnd('/')}/v3/conversations/{conversationId}/activities/{activityId}";
        string body = activity.ToJson();

        logger.LogTrace("Updating activity at {Url}: {Activity}", url, body);

        return await SendHttpRequestAsync<UpdateActivityResponse>(
            HttpMethod.Put,
            url,
            body,
            activity.From.GetAgenticIdentity(),
            "updating activity",
            customHeaders,
            cancellationToken).ConfigureAwait(false);
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
        ArgumentNullException.ThrowIfNullOrWhiteSpace(conversationId);
        ArgumentNullException.ThrowIfNullOrWhiteSpace(activityId);
        ArgumentNullException.ThrowIfNull(serviceUrl);

        string url = $"{serviceUrl.ToString().TrimEnd('/')}/v3/conversations/{conversationId}/activities/{activityId}";

        logger.LogTrace("Deleting activity at {Url}", url);

        await SendHttpRequestAsync<DeleteActivityResponse>(
            HttpMethod.Delete,
            url,
            body: null,
            agenticIdentity: agenticIdentity,
            "deleting activity",
            customHeaders,
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
        ArgumentNullException.ThrowIfNullOrWhiteSpace(activity.Id);
        ArgumentNullException.ThrowIfNull(activity.Conversation);
        ArgumentNullException.ThrowIfNullOrWhiteSpace(activity.Conversation.Id);
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
        ArgumentNullException.ThrowIfNullOrWhiteSpace(conversationId);
        ArgumentNullException.ThrowIfNull(serviceUrl);

        string url = $"{serviceUrl.ToString().TrimEnd('/')}/v3/conversations/{conversationId}/members";

        logger.LogTrace("Getting conversation members from {Url}", url);

        return await SendHttpRequestAsync<IList<ConversationAccount>>(
            HttpMethod.Get,
            url,
            body: null,
            agenticIdentity,
            "getting conversation members",
            customHeaders,
            cancellationToken).ConfigureAwait(false);
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

        return await SendHttpRequestAsync<GetConversationsResponse>(
            HttpMethod.Get,
            url,
            body: null,
            agenticIdentity,
            "getting conversations",
            customHeaders,
            cancellationToken).ConfigureAwait(false);
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
        ArgumentNullException.ThrowIfNullOrWhiteSpace(conversationId);
        ArgumentNullException.ThrowIfNullOrWhiteSpace(activityId);
        ArgumentNullException.ThrowIfNull(serviceUrl);

        string url = $"{serviceUrl.ToString().TrimEnd('/')}/v3/conversations/{conversationId}/activities/{activityId}/members";

        logger.LogTrace("Getting activity members from {Url}", url);

        return await SendHttpRequestAsync<IList<ConversationAccount>>(
            HttpMethod.Get,
            url,
            body: null,
            agenticIdentity,
            "getting activity members",
            customHeaders,
            cancellationToken).ConfigureAwait(false);
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

        return await SendHttpRequestAsync<CreateConversationResponse>(
            HttpMethod.Post,
            url,
            JsonSerializer.Serialize(parameters),
            agenticIdentity,
            "creating conversation",
            customHeaders,
            cancellationToken).ConfigureAwait(false);
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
        ArgumentNullException.ThrowIfNullOrWhiteSpace(conversationId);
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

        return await SendHttpRequestAsync<PagedMembersResult>(
            HttpMethod.Get,
            url,
            body: null,
            agenticIdentity,
            "getting paged conversation members",
            customHeaders,
            cancellationToken).ConfigureAwait(false);
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
        ArgumentNullException.ThrowIfNullOrWhiteSpace(conversationId);
        ArgumentNullException.ThrowIfNullOrWhiteSpace(memberId);
        ArgumentNullException.ThrowIfNull(serviceUrl);

        string url = $"{serviceUrl.ToString().TrimEnd('/')}/v3/conversations/{conversationId}/members/{memberId}";

        logger.LogTrace("Deleting conversation member at {Url}", url);

        await SendHttpRequestAsync<object>(
            HttpMethod.Delete,
            url,
            body: null,
            agenticIdentity,
            "deleting conversation member",
            customHeaders,
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
        ArgumentNullException.ThrowIfNullOrWhiteSpace(conversationId);
        ArgumentNullException.ThrowIfNull(transcript);
        ArgumentNullException.ThrowIfNull(serviceUrl);

        string url = $"{serviceUrl.ToString().TrimEnd('/')}/v3/conversations/{conversationId}/activities/history";

        logger.LogTrace("Sending conversation history to {Url}: {Transcript}", url, JsonSerializer.Serialize(transcript));

        return await SendHttpRequestAsync<SendConversationHistoryResponse>(
            HttpMethod.Post,
            url,
            JsonSerializer.Serialize(transcript),
            agenticIdentity,
            "sending conversation history",
            customHeaders,
            cancellationToken).ConfigureAwait(false);
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
        ArgumentNullException.ThrowIfNullOrWhiteSpace(conversationId);
        ArgumentNullException.ThrowIfNull(attachmentData);
        ArgumentNullException.ThrowIfNull(serviceUrl);

        string url = $"{serviceUrl.ToString().TrimEnd('/')}/v3/conversations/{conversationId}/attachments";
        
        logger.LogTrace("Uploading attachment to {Url}: {AttachmentData}", url, JsonSerializer.Serialize(attachmentData));

        return await SendHttpRequestAsync<UploadAttachmentResponse>(
            HttpMethod.Post,
            url,
            JsonSerializer.Serialize(attachmentData),
            agenticIdentity,
            "uploading attachment",
            customHeaders,
            cancellationToken).ConfigureAwait(false);
    }

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
                T? result = JsonSerializer.Deserialize<T>(responseString);
                return result ?? throw new InvalidOperationException($"Failed to deserialize response for {operationDescription}");
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

}
