// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Common.Http;

namespace Microsoft.Teams.Api.Clients;

public class BotSignInClient : Client
{
    private readonly ApiClientOptions _apiClientOptions;

    // Bot sign-in API endpoints
    private const string BOT_SIGNIN_GET_URL = "api/botsignin/GetSignInUrl";
    private const string BOT_SIGNIN_GET_RESOURCE = "api/botsignin/GetSignInResource";

    public BotSignInClient() : base()
    {
        _apiClientOptions = ApiClientOptions.Merge();
    }

    public BotSignInClient(IHttpClient client, CancellationToken cancellationToken = default) : base(client, cancellationToken)
    {
        _apiClientOptions = ApiClientOptions.Merge();
    }

    public BotSignInClient(IHttpClientOptions options, CancellationToken cancellationToken = default) : base(options, cancellationToken)
    {
        _apiClientOptions = ApiClientOptions.Merge();
    }

    public BotSignInClient(IHttpClientFactory factory, CancellationToken cancellationToken = default) : base(factory, cancellationToken)
    {
        _apiClientOptions = ApiClientOptions.Merge();
    }

    public BotSignInClient(IHttpClient client, ApiClientOptions? apiClientOptions, CancellationToken cancellationToken = default) : base(client, cancellationToken)
    {
        _apiClientOptions = ApiClientOptions.Merge(apiClientOptions);
    }

    public BotSignInClient(IHttpClientOptions options, ApiClientOptions? apiClientOptions, CancellationToken cancellationToken = default) : base(options, cancellationToken)
    {
        _apiClientOptions = ApiClientOptions.Merge(apiClientOptions);
    }

    public BotSignInClient(IHttpClientFactory factory, ApiClientOptions? apiClientOptions, CancellationToken cancellationToken = default) : base(factory, cancellationToken)
    {
        _apiClientOptions = ApiClientOptions.Merge(apiClientOptions);
    }

    public async Task<string> GetUrlAsync(GetUrlRequest request)
    {
        var query = QueryString.Serialize(request);
        var req = HttpRequest.Get(
            $"{_apiClientOptions.OAuthUrl}/{BOT_SIGNIN_GET_URL}?{query}"
        );

        var res = await _http.SendAsync(req, _cancellationToken);
        return res.Body;
    }

    public async Task<SignIn.UrlResponse> GetResourceAsync(GetResourceRequest request)
    {
        var query = QueryString.Serialize(request);
        var req = HttpRequest.Get(
            $"{_apiClientOptions.OAuthUrl}/{BOT_SIGNIN_GET_RESOURCE}?{query}"
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