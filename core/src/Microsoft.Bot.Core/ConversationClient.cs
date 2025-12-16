// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net.Mime;
using System.Text;
using System.Text.Json;
using Microsoft.Bot.Core.Hosting;
using Microsoft.Bot.Core.Schema;
using Microsoft.Extensions.Logging;

namespace Microsoft.Bot.Core;

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
        string body = activity.ToJson();

        logger.LogTrace("Sending activity to {Url}: {Activity}", url, body);

        using StringContent content = new(body, Encoding.UTF8, MediaTypeNames.Application.Json);

        using HttpRequestMessage request = new(HttpMethod.Post, url) { Content = content };

        request.Options.Set(BotAuthenticationHandler.AgenticIdentityKey, AgenticIdentity.FromProperties(activity.From.Properties));

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
            throw new HttpRequestException($"Error sending activity {resp.StatusCode}. {errResponseString}");
        }
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
