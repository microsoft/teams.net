using System.Text.Json;
using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Common.Http;

namespace Microsoft.Teams.Api.Clients;

public class ChannelActivityClient : Client
{
    public readonly string ServiceUrl;

    public ChannelActivityClient(string serviceUrl, CancellationToken cancellationToken = default) : base(cancellationToken)
    {
        ServiceUrl = serviceUrl;
    }

    public ChannelActivityClient(string serviceUrl, IHttpClient client, CancellationToken cancellationToken = default) : base(client, cancellationToken)
    {
        ServiceUrl = serviceUrl;
    }

    public ChannelActivityClient(string serviceUrl, IHttpClientOptions options, CancellationToken cancellationToken = default) : base(options, cancellationToken)
    {
        ServiceUrl = serviceUrl;
    }

    public ChannelActivityClient(string serviceUrl, IHttpClientFactory factory, CancellationToken cancellationToken = default) : base(factory, cancellationToken)
    {
        ServiceUrl = serviceUrl;
    }

    public async Task<Resource?> CreateAsync(string channelId, IActivity activity)
    {
        var req = HttpRequest.Post(
            $"{ServiceUrl}v3/conversations/{channelId}/activities",
            body: activity
        );

        var res = await _http.SendAsync(req, _cancellationToken);

        if (res.Body == string.Empty) return null;

        var body = JsonSerializer.Deserialize<Resource>(res.Body);
        return body;
    }

    public async Task<Resource?> UpdateAsync(string channelId, string id, IActivity activity)
    {
        var req = HttpRequest.Put(
            $"{ServiceUrl}v3/conversations/{channelId}/activities/{id}",
            body: activity
        );

        var res = await _http.SendAsync(req, _cancellationToken);

        if (res.Body == string.Empty) return null;

        var body = JsonSerializer.Deserialize<Resource>(res.Body);
        return body;
    }

    public async Task DeleteAsync(string channelId, string id)
    {
        var req = HttpRequest.Delete(
            $"{ServiceUrl}v3/conversations/{channelId}/activities/{id}"
        );

        await _http.SendAsync(req, _cancellationToken);
    }
}
