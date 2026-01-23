// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Api.Messages;
using Microsoft.Teams.Common.Http;

namespace Microsoft.Teams.Api.Clients;

/// <summary>
/// Client for working with app message reactions for a given conversation/activity.
/// </summary>
public class ReactionClient : Client
{
    public readonly string ServiceUrl;

    public ReactionClient(string serviceUrl, CancellationToken cancellationToken = default)
        : base(cancellationToken)
    {
        ServiceUrl = serviceUrl;
    }

    public ReactionClient(string serviceUrl, IHttpClient client, CancellationToken cancellationToken = default)
        : base(client, cancellationToken)
    {
        ServiceUrl = serviceUrl;
    }

    public ReactionClient(string serviceUrl, IHttpClientOptions options, CancellationToken cancellationToken = default)
        : base(options, cancellationToken)
    {
        ServiceUrl = serviceUrl;
    }

    public ReactionClient(string serviceUrl, IHttpClientFactory factory, CancellationToken cancellationToken = default)
        : base(factory, cancellationToken)
    {
        ServiceUrl = serviceUrl;
    }

    /// <summary>
    /// Creates or updates a reaction on an activity in a conversation.
    /// </summary>
    /// <param name="conversationId">The conversation id.</param>
    /// <param name="activityId">The id of the activity to react to.</param>
    /// <param name="reactionType">
    /// The reaction type (for example: "like", "heart", "laugh", etc.).
    /// </param>
    /// <param name="userId">
    /// Optional id of the user on whose behalf the reaction is added/updated (if supported by the service).
    /// </param>
    /// <returns>
    /// A <see cref="Resource"/> describing the reaction, or <c>null</c> if the service returned an empty body.
    /// </returns>
    public async Task CreateOrUpdateAsync(
        string conversationId,
        string activityId,
        ReactionType reactionType
    )
    {
        // Assumed route:
        //   PUT v3/conversations/{conversationId}/activities/{activityId}/reactions
        var url = $"{ServiceUrl}v3/conversations/{conversationId}/activities/{activityId}/reactions/{reactionType}";
        var req = HttpRequest.Put(url);
        await _http.SendAsync(req, _cancellationToken);
    }

    /// <summary>
    /// Removes a reaction from an activity in a conversation.
    /// </summary>
    /// <param name="conversationId">The conversation id.</param>
    /// <param name="activityId">The id of the activity the reaction is on.</param>
    /// <param name="reactionType">
    /// The reaction type to remove (for example: "like", "heart", "laugh", etc.).
    /// </param>
    /// <param name="userId">
    /// Optional id of the user whose reaction should be removed (if supported by the service).
    /// </param>
    public async Task DeleteAsync(
        string conversationId,
        string activityId,
        ReactionType reactionType
    )
    {
        // Assumed route:
        //   DELETE v3/conversations/{conversationId}/activities/{activityId}/reactions/{reactionType}
        var url =
            $"{ServiceUrl}v3/conversations/{conversationId}/activities/{activityId}/reactions/{reactionType}";

        var req = HttpRequest.Delete(url);

        await _http.SendAsync(req, _cancellationToken);
    }
}
