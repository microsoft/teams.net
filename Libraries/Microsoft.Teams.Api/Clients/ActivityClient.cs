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
    public async Task<Resource?> SendTargetedAsync(string userId, string conversationId, IActivity activity)
    {
        var url = !string.IsNullOrEmpty(activity.ReplyToId)
                ? $"{ServiceUrl}v3/users/{userId}/conversations/{conversationId}/targetedactivities/{activity.ReplyToId}"
                : $"{ServiceUrl}v3/users/{userId}/conversations/{conversationId}/targetedactivities";

        var req = HttpRequest.Post(url, body: activity);
        var res = await _http.SendAsync(req, _cancellationToken);

        if (res.Body == string.Empty) return null;
        
        var body = JsonSerializer.Deserialize<Resource>(res.Body);
        return body;
    }
    
    public async Task<Resource?> UpdateTargetedAsync(string userId, string conversationId, string activityId, IActivity activity)
    {
        var req = HttpRequest.Put(
            $"{ServiceUrl}v3/users/{userId}/conversations/{conversationId}/targetedactivities/{activityId}",
            body: activity
        );

        var res = await _http.SendAsync(req, _cancellationToken);

        if (res.Body == string.Empty) return null;
        
        var body = JsonSerializer.Deserialize<Resource>(res.Body);
        return body;
    }
}