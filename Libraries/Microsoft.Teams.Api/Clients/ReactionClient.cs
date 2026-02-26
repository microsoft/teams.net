// Copyright (c) Microsoft Corporation. All rights reserved.
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
    /// Adds a reaction on an activity in a conversation.
    /// </summary>
    /// <param name="conversationId">The conversation id.</param>
    /// <param name="activityId">The id of the activity to react to.</param>
    /// <param name="reactionType">
    /// The reaction type (for example: "like", "heart", "launch", etc.).
    /// </param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>
    /// A <see cref="Task"/> representing the asynchronous operation.
    /// </returns>
    public async Task AddAsync(
        string conversationId,
        string activityId,
        ReactionType reactionType,
        CancellationToken cancellationToken = default
    )
    {
        // Assumed route:
        //   PUT v3/conversations/{conversationId}/activities/{activityId}/reactions/{reactionType}
        var url = $"{ServiceUrl}v3/conversations/{conversationId}/activities/{activityId}/reactions/{reactionType}";
        var req = HttpRequest.Put(url);
        await _http.SendAsync(req, cancellationToken != default ? cancellationToken : _cancellationToken);
    }

    /// <summary>
    /// Removes a reaction from an activity in a conversation.
    /// </summary>
    /// <param name="conversationId">The conversation id.</param>
    /// <param name="activityId">The id of the activity the reaction is on.</param>
    /// <param name="reactionType">
    /// The reaction type to remove (for example: "like", "heart", "launch", etc.).
    /// </param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>
    /// A <see cref="Task"/> representing the asynchronous operation.
    /// </returns>
    public async Task DeleteAsync(
        string conversationId,
        string activityId,
        ReactionType reactionType,
        CancellationToken cancellationToken = default
    )
    {
        // Assumed route:
        //   DELETE v3/conversations/{conversationId}/activities/{activityId}/reactions/{reactionType}
        var url =
            $"{ServiceUrl}v3/conversations/{conversationId}/activities/{activityId}/reactions/{reactionType}";

        var req = HttpRequest.Delete(url);

        await _http.SendAsync(req, cancellationToken != default ? cancellationToken : _cancellationToken);
    }
}
