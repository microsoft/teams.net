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

    public virtual async Task<Resource?> CreateAsync(string conversationId, IActivity activity, bool isTargeted = false)
    {
        var url = $"{ServiceUrl}v3/conversations/{conversationId}/activities";
        if (isTargeted)
        {
            url += "?isTargetedActivity=true";
        }

        var req = HttpRequest.Post(url, body: activity);

        var res = await _http.SendAsync(req, _cancellationToken);

        if (res.Body == string.Empty) return null;

        var body = JsonSerializer.Deserialize<Resource>(res.Body);
        return body;
    }

    public virtual async Task<Resource?> UpdateAsync(string conversationId, string id, IActivity activity, bool isTargeted = false)
    {
        var url = $"{ServiceUrl}v3/conversations/{conversationId}/activities/{id}";
        if (isTargeted)
        {
            url += "?isTargetedActivity=true";
        }

        var req = HttpRequest.Put(url, body: activity);

        var res = await _http.SendAsync(req, _cancellationToken);

        if (res.Body == string.Empty) return null;

        var body = JsonSerializer.Deserialize<Resource>(res.Body);
        return body;
    }

    public virtual async Task<Resource?> ReplyAsync(string conversationId, string id, IActivity activity, bool isTargeted = false)
    {
        activity.ReplyToId = id;

        var url = $"{ServiceUrl}v3/conversations/{conversationId}/activities/{id}";
        if (isTargeted)
        {
            url += "?isTargetedActivity=true";
        }

        var req = HttpRequest.Post(url, body: activity);

        var res = await _http.SendAsync(req, _cancellationToken);

        if (res.Body == string.Empty) return null;

        var body = JsonSerializer.Deserialize<Resource>(res.Body);
        return body;
    }

    public virtual async Task DeleteAsync(string conversationId, string id, bool isTargeted = false)
    {
        var url = $"{ServiceUrl}v3/conversations/{conversationId}/activities/{id}";
        if (isTargeted)
        {
            url += "?isTargetedActivity=true";
        }

        var req = HttpRequest.Delete(url);

        await _http.SendAsync(req, _cancellationToken);
    }
}