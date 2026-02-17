// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;

using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Common.Http;

namespace Microsoft.Teams.Api.Clients;

public class ActivityClient : Client
{
    public readonly string ServiceUrl;

    public ActivityClient(string serviceUrl, CancellationToken cancellationToken = default) : base(cancellationToken)
    {
        ServiceUrl = serviceUrl;
    }

    public ActivityClient(string serviceUrl, IHttpClient client, CancellationToken cancellationToken = default) : base(client, cancellationToken)
    {
        ServiceUrl = serviceUrl;
    }

    public ActivityClient(string serviceUrl, IHttpClientOptions options, CancellationToken cancellationToken = default) : base(options, cancellationToken)
    {
        ServiceUrl = serviceUrl;
    }

    public ActivityClient(string serviceUrl, IHttpClientFactory factory, CancellationToken cancellationToken = default) : base(factory, cancellationToken)
    {
        ServiceUrl = serviceUrl;
    }

    public async Task<Resource?> CreateAsync(string conversationId, IActivity activity)
    {
        var req = HttpRequest.Post(
            $"{ServiceUrl}v3/conversations/{conversationId}/activities",
            body: activity
        );

        var res = await _http.SendAsync(req, _cancellationToken);

        if (res.Body == string.Empty) return null;

        var body = JsonSerializer.Deserialize<Resource>(res.Body);
        return body;
    }

    public async Task<Resource?> UpdateAsync(string conversationId, string id, IActivity activity)
    {
        var req = HttpRequest.Put(
            $"{ServiceUrl}v3/conversations/{conversationId}/activities/{id}",
            body: activity
        );

        var res = await _http.SendAsync(req, _cancellationToken);

        if (res.Body == string.Empty) return null;

        var body = JsonSerializer.Deserialize<Resource>(res.Body);
        return body;
    }

    public async Task<Resource?> ReplyAsync(string conversationId, string id, IActivity activity)
    {
        activity.ReplyToId = id;
        var req = HttpRequest.Post(
            $"{ServiceUrl}v3/conversations/{conversationId}/activities/{id}",
            body: activity
        );

        var res = await _http.SendAsync(req, _cancellationToken);

        if (res.Body == string.Empty) return null;

        var body = JsonSerializer.Deserialize<Resource>(res.Body);
        return body;
    }

    public async Task DeleteAsync(string conversationId, string id)
    {
        var req = HttpRequest.Delete(
            $"{ServiceUrl}v3/conversations/{conversationId}/activities/{id}"
        );

        await _http.SendAsync(req, _cancellationToken);
    }

    /// <summary>
    /// Create a new targeted activity in a conversation.
    /// Targeted activities are only visible to the specified recipient.
    /// </summary>
    /// <param name="conversationId">The ID of the conversation</param>
    /// <param name="activity">The activity to create</param>
    /// <returns>The created activity resource</returns>
    public async Task<Resource?> CreateTargetedAsync(string conversationId, IActivity activity)
    {
        var req = HttpRequest.Post(
            $"{ServiceUrl}v3/conversations/{conversationId}/activities?isTargetedActivity=true",
            body: activity
        );

        var res = await _http.SendAsync(req, _cancellationToken);

        if (res.Body == string.Empty) return null;

        var body = JsonSerializer.Deserialize<Resource>(res.Body);
        return body;
    }

    /// <summary>
    /// Update an existing targeted activity in a conversation.
    /// </summary>
    /// <param name="conversationId">The ID of the conversation</param>
    /// <param name="id">The ID of the activity to update</param>
    /// <param name="activity">The updated activity data</param>
    /// <returns>The updated activity resource</returns>
    public async Task<Resource?> UpdateTargetedAsync(string conversationId, string id, IActivity activity)
    {
        var req = HttpRequest.Put(
            $"{ServiceUrl}v3/conversations/{conversationId}/activities/{id}?isTargetedActivity=true",
            body: activity
        );

        var res = await _http.SendAsync(req, _cancellationToken);

        if (res.Body == string.Empty) return null;

        var body = JsonSerializer.Deserialize<Resource>(res.Body);
        return body;
    }

    /// <summary>
    /// Delete a targeted activity from a conversation.
    /// </summary>
    /// <param name="conversationId">The ID of the conversation</param>
    /// <param name="id">The ID of the activity to delete</param>
    public async Task DeleteTargetedAsync(string conversationId, string id)
    {
        var req = HttpRequest.Delete(
            $"{ServiceUrl}v3/conversations/{conversationId}/activities/{id}?isTargetedActivity=true"
        );

        await _http.SendAsync(req, _cancellationToken);
    }
}