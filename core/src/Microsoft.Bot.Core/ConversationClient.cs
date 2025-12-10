using System.Net.Http.Json;

using Microsoft.Bot.Core.Hosting;
using Microsoft.Bot.Core.Schema;

namespace Microsoft.Bot.Core;

/// <summary>
/// Provides methods for sending activities to a conversation endpoint using HTTP requests.
/// </summary>
/// <param name="httpClient">The HTTP client instance used to send requests to the conversation service. Must not be null.</param>
public class ConversationClient(HttpClient httpClient)
{
    internal const string ConversationHttpClientName = "BotFrameworkConversation";

    internal AgenticIdentity? AgenticIdentity { get; set; }

    /// <summary>
    /// Sends the specified activity to the conversation endpoint asynchronously.
    /// </summary>
    /// <param name="activity">The activity to send. Cannot be null. The activity must contain valid conversation and service URL information.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the send operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the response content as a string if
    /// the activity is sent successfully.</returns>
    /// <exception cref="Exception">Thrown if the activity could not be sent successfully. The exception message includes the HTTP status code and
    /// response content.</exception>
    public async Task<string> SendActivityAsync(CoreActivity activity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);
        ArgumentNullException.ThrowIfNull(activity.Conversation);
        ArgumentNullException.ThrowIfNull(activity.Conversation.Id);
        ArgumentNullException.ThrowIfNull(activity.ServiceUrl);

        using HttpRequestMessage request = new(
            HttpMethod.Post,
            $"{activity.ServiceUrl.ToString().TrimEnd('/')}/v3/conversations/{activity.Conversation.Id}/activities/")
        {
            Content = JsonContent.Create(activity, options: CoreActivity.DefaultJsonOptions),
        };

        request.Options.Set(BotAuthenticationHandler.AgenticIdentityKey, AgenticIdentity.FromProperties(activity.From?.Properties));

        using HttpResponseMessage resp = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

        string respContent = await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        return resp.IsSuccessStatusCode ?
            respContent :
            throw new HttpRequestException($"Error sending activity: {resp.StatusCode} - {respContent}");
    }
}