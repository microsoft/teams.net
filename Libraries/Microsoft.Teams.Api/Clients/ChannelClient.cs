using Microsoft.Teams.Common.Http;

namespace Microsoft.Teams.Api.Clients;

public class ChannelClient : Client
{
    public readonly string ServiceUrl;
    public readonly ChannelActivityClient Activities;

    public ChannelClient(string serviceUrl, CancellationToken cancellationToken = default) : base(cancellationToken)
    {
        ServiceUrl = serviceUrl;
        Activities = new ChannelActivityClient(serviceUrl, _http, cancellationToken);
    }

    public ChannelClient(string serviceUrl, IHttpClient client, CancellationToken cancellationToken = default) : base(client, cancellationToken)
    {
        ServiceUrl = serviceUrl;
        Activities = new ChannelActivityClient(serviceUrl, _http, cancellationToken);
    }

    public ChannelClient(string serviceUrl, IHttpClientOptions options, CancellationToken cancellationToken = default) : base(options, cancellationToken)
    {
        ServiceUrl = serviceUrl;
        Activities = new ChannelActivityClient(serviceUrl, _http, cancellationToken);
    }

    public ChannelClient(string serviceUrl, IHttpClientFactory factory, CancellationToken cancellationToken = default) : base(factory, cancellationToken)
    {
        ServiceUrl = serviceUrl;
        Activities = new ChannelActivityClient(serviceUrl, _http, cancellationToken);
    }
}
