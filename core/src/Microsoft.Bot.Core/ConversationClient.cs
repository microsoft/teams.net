// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net.Http.Json;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using Microsoft.Bot.Core.Hosting;
using Microsoft.Bot.Core.Schema;

namespace Microsoft.Bot.Core;

/// <summary>
/// Provides methods for sending activities to a conversation endpoint using HTTP requests.
/// </summary>
/// <param name="httpClient">The HTTP client instance used to send requests to the conversation service. Must not be null.</param>
public class ConversationClient(HttpClient httpClient)
{
    internal const string ConversationHttpClientName = "BotConversationClient";

    /// <summary>
    /// Sends the specified activity to the conversation endpoint asynchronously.
    /// </summary>
    /// <param name="activity">The activity to send. Cannot be null. The activity must contain valid conversation and service URL information.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the send operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the response content as a string if
    /// the activity is sent successfully.</returns>
    /// <exception cref="Exception">Thrown if the activity could not be sent successfully. The exception message includes the HTTP status code and
    /// response content.</exception>
    public async Task<ResourceResponse> SendActivityAsync(CoreActivity activity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);
        ArgumentNullException.ThrowIfNull(activity.Conversation);
        ArgumentNullException.ThrowIfNullOrWhiteSpace(activity.Conversation.Id);
        ArgumentNullException.ThrowIfNull(activity.ServiceUrl);

        string url = $"{activity.ServiceUrl.ToString().TrimEnd('/')}/v3/conversations/{activity.Conversation.Id}/activities/";

        return await SendHttpRequestAsync(HttpMethod.Post, url, activity, "sending activity", cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Updates an existing activity in a conversation.
    /// </summary>
    /// <param name="conversationId">The ID of the conversation. Cannot be null or whitespace.</param>
    /// <param name="activityId">The ID of the activity to update. Cannot be null or whitespace.</param>
    /// <param name="activity">The updated activity data. Cannot be null.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the update operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the response with the ID of the updated activity.</returns>
    /// <exception cref="HttpRequestException">Thrown if the activity could not be updated successfully.</exception>
    public async Task<ResourceResponse> UpdateActivityAsync(string conversationId, string activityId, CoreActivity activity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(conversationId);
        ArgumentNullException.ThrowIfNullOrWhiteSpace(activityId);
        ArgumentNullException.ThrowIfNull(activity);
        ArgumentNullException.ThrowIfNull(activity.ServiceUrl);

        string url = $"{activity.ServiceUrl.ToString().TrimEnd('/')}/v3/conversations/{conversationId}/activities/{activityId}";

        return await SendHttpRequestAsync(HttpMethod.Put, url, activity, "updating activity", cancellationToken).ConfigureAwait(false);
    }


    /// <summary>
    /// Deletes an existing activity from a conversation.
    /// </summary>
    /// <param name="conversationId">The ID of the conversation. Cannot be null or whitespace.</param>
    /// <param name="activityId">The ID of the activity to delete. Cannot be null or whitespace.</param>
    /// <param name="serviceUrl">The service URL for the conversation. Cannot be null.</param>
    /// <param name="agenticIdentity">Optional agentic identity for authentication.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the delete operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="HttpRequestException">Thrown if the activity could not be deleted successfully.</exception>
    public async Task DeleteActivityAsync(string conversationId, string activityId, Uri serviceUrl, AgenticIdentity? agenticIdentity = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(conversationId);
        ArgumentNullException.ThrowIfNullOrWhiteSpace(activityId);
        ArgumentNullException.ThrowIfNull(serviceUrl);

        string url = $"{serviceUrl.ToString().TrimEnd('/')}/v3/conversations/{conversationId}/activities/{activityId}";

        await SendHttpRequestAsync(
            HttpMethod.Delete,
            url,
            body: null,
            agenticIdentity: agenticIdentity,
            "deleting activity",
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Deletes an existing activity from a conversation using activity context.
    /// </summary>
    /// <param name="activity">The activity to delete. Must contain valid Id, Conversation.Id, and ServiceUrl. Cannot be null.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the delete operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="HttpRequestException">Thrown if the activity could not be deleted successfully.</exception>
    public async Task DeleteActivityAsync(CoreActivity activity, CancellationToken cancellationToken = default)
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
            cancellationToken).ConfigureAwait(false);
    }

    private async Task<ResourceResponse> SendHttpRequestAsync(HttpMethod method, string url, string? body, AgenticIdentity? agenticIdentity, string operationDescription, CancellationToken cancellationToken)
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

        using HttpResponseMessage resp = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

        if (resp.IsSuccessStatusCode)
        {
            string responseString = await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            if (responseString.Length > 2) // to handle empty response
            {
                ResourceResponse? resourceResponse = JsonSerializer.Deserialize<ResourceResponse>(responseString);
                return resourceResponse ?? new ResourceResponse();
            }
            return new ResourceResponse();
        }
        else
        {
            string errResponseString = await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            throw new HttpRequestException($"Error {operationDescription} {resp.StatusCode}. {errResponseString}");
        }
    }

    private Task<ResourceResponse> SendHttpRequestAsync(HttpMethod method, string url, CoreActivity activity, string operationDescription, CancellationToken cancellationToken)
    {
        return SendHttpRequestAsync(
            method,
            url,
            activity.ToJson(),
            activity.From.GetAgenticIdentity(),
            operationDescription,
            cancellationToken);
    }

}

/// <summary>
/// Resource Response
/// </summary>
public class ResourceResponse
{
    /// <summary>
    /// Id of the activity
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }
}
