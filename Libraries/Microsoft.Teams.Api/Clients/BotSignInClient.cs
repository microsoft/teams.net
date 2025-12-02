// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Common.Http;

namespace Microsoft.Teams.Api.Clients;

public class BotSignInClient : Client
{
    public BotSignInClient() : base()
    {

    }

    public BotSignInClient(ICustomHttpClient client, CancellationToken cancellationToken = default) : base(client, cancellationToken)
    {

    }

    public BotSignInClient(IHttpClientOptions options, CancellationToken cancellationToken = default) : base(options, cancellationToken)
    {

    }

    public BotSignInClient(ICustomHttpClientFactory factory, CancellationToken cancellationToken = default) : base(factory, cancellationToken)
    {

    }

    public async Task<string> GetUrlAsync(GetUrlRequest request)
    {
        var query = QueryString.Serialize(request);
        var req = HttpRequest.Get(
            $"https://token.botframework.com/api/botsignin/GetSignInUrl?{query}"
        );

        var res = await _http.SendAsync(req, _cancellationToken);
        return res.Body;
    }

    public async Task<SignIn.UrlResponse> GetResourceAsync(GetResourceRequest request)
    {
        var query = QueryString.Serialize(request);
        var req = HttpRequest.Get(
            $"https://token.botframework.com/api/botsignin/GetSignInResource?{query}"
        );

        var res = await _http.SendAsync<SignIn.UrlResponse>(req, _cancellationToken);
        return res.Body;
    }

    public class GetUrlRequest
    {
        public required string State { get; set; }
        public string? CodeChallenge { get; set; }
        public string? EmulatorUrl { get; set; }
        public string? FinalRedirect { get; set; }
    }

    public class GetResourceRequest
    {
        public required string State { get; set; }
        public string? CodeChallenge { get; set; }
        public string? EmulatorUrl { get; set; }
        public string? FinalRedirect { get; set; }
    }
}