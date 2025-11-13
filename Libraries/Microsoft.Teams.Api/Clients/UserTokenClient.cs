// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Teams.Common.Http;

namespace Microsoft.Teams.Api.Clients;

public class UserTokenClient : Client
{
    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly ApiClientOptions _apiClientSettings;

    // User token API endpoints
    private const string USER_TOKEN_GET_TOKEN = "api/usertoken/GetToken";
    private const string USER_TOKEN_GET_AAD_TOKENS = "api/usertoken/GetAadTokens";
    private const string USER_TOKEN_GET_STATUS = "api/usertoken/GetTokenStatus";
    private const string USER_TOKEN_SIGN_OUT = "api/usertoken/SignOut";
    private const string USER_TOKEN_EXCHANGE = "api/usertoken/exchange";

    public UserTokenClient(CancellationToken cancellationToken = default) : base(cancellationToken)
    {
        _apiClientSettings = ApiClientOptions.Merge();
    }

    public UserTokenClient(IHttpClient client, CancellationToken cancellationToken = default) : base(client, cancellationToken)
    {
        _apiClientSettings = ApiClientOptions.Merge();
    }

    public UserTokenClient(IHttpClientOptions options, CancellationToken cancellationToken = default) : base(options, cancellationToken)
    {
        _apiClientSettings = ApiClientOptions.Merge();
    }

    public UserTokenClient(IHttpClientFactory factory, CancellationToken cancellationToken = default) : base(factory, cancellationToken)
    {
        _apiClientSettings = ApiClientOptions.Merge();
    }

    public UserTokenClient(IHttpClient client, ApiClientOptions? apiClientSettings, CancellationToken cancellationToken = default) : base(client, cancellationToken)
    {
        _apiClientSettings = ApiClientOptions.Merge(apiClientSettings);
    }

    public UserTokenClient(IHttpClientOptions options, ApiClientOptions? apiClientSettings, CancellationToken cancellationToken = default) : base(options, cancellationToken)
    {
        _apiClientSettings = ApiClientOptions.Merge(apiClientSettings);
    }

    public UserTokenClient(IHttpClientFactory factory, ApiClientOptions? apiClientSettings, CancellationToken cancellationToken = default) : base(factory, cancellationToken)
    {
        _apiClientSettings = ApiClientOptions.Merge(apiClientSettings);
    }

    public async Task<Token.Response> GetAsync(GetTokenRequest request)
    {
        var query = QueryString.Serialize(request);
        var req = HttpRequest.Get($"{_apiClientSettings.OAuthUrl}/{USER_TOKEN_GET_TOKEN}?{query}");
        var res = await _http.SendAsync<Token.Response>(req, _cancellationToken);
        return res.Body;
    }

    public async Task<IDictionary<string, Token.Response>> GetAadAsync(GetAadTokenRequest request)
    {
        var query = QueryString.Serialize(request);
        var req = HttpRequest.Post($"{_apiClientSettings.OAuthUrl}/{USER_TOKEN_GET_AAD_TOKENS}?{query}", body: request);
        var res = await _http.SendAsync<IDictionary<string, Token.Response>>(req, _cancellationToken);
        return res.Body;
    }

    public async Task<IList<Token.Status>> GetStatusAsync(GetTokenStatusRequest request)
    {
        var query = QueryString.Serialize(request);
        var req = HttpRequest.Get($"{_apiClientSettings.OAuthUrl}/{USER_TOKEN_GET_STATUS}?{query}");
        var res = await _http.SendAsync<IList<Token.Status>>(req, _cancellationToken);
        return res.Body;
    }

    public async Task SignOutAsync(SignOutRequest request)
    {
        var query = QueryString.Serialize(request);
        var req = HttpRequest.Delete($"{_apiClientSettings.OAuthUrl}/{USER_TOKEN_SIGN_OUT}?{query}");
        await _http.SendAsync(req, _cancellationToken);
    }

    public async Task<Token.Response> ExchangeAsync(ExchangeTokenRequest request)
    {
        var query = QueryString.Serialize(new
        {
            userId = request.UserId,
            connectionName = request.ConnectionName,
            channelId = request.ChannelId
        });

        // This ensures that the request body is buffered so that when sent the `Content-Length` header is set.
        // This is required for the Bot Framework Token Service to process the request correctly.
        var body = JsonSerializer.Serialize(request.GetBody(), _jsonSerializerOptions);

        var req = HttpRequest.Post($"{_apiClientSettings.OAuthUrl}/{USER_TOKEN_EXCHANGE}?{query}", body);
        req.Headers.Add("Content-Type", new List<string>() { "application/json" });

        var res = await _http.SendAsync<Token.Response>(req, _cancellationToken);
        return res.Body;
    }

    public class GetTokenRequest
    {
        [JsonPropertyName("userId")]
        [JsonPropertyOrder(0)]
        public required string UserId { get; set; }

        [JsonPropertyName("connectionName")]
        [JsonPropertyOrder(1)]
        public required string ConnectionName { get; set; }

        [JsonPropertyName("channelId")]
        [JsonPropertyOrder(2)]
        public ChannelId? ChannelId { get; set; }

        [JsonPropertyName("code")]
        [JsonPropertyOrder(3)]
        public string? Code { get; set; }
    }

    public class GetAadTokenRequest
    {
        [JsonPropertyName("userId")]
        [JsonPropertyOrder(0)]
        public required string UserId { get; set; }

        [JsonPropertyName("connectionName")]
        [JsonPropertyOrder(1)]
        public required string ConnectionName { get; set; }

        [JsonPropertyName("channelId")]
        [JsonPropertyOrder(2)]
        public required ChannelId ChannelId { get; set; }

        [JsonPropertyName("resourceUrls")]
        [JsonPropertyOrder(3)]
        public IList<string> ResourceUrls { get; set; } = [];
    }

    public class GetTokenStatusRequest
    {
        [JsonPropertyName("userId")]
        [JsonPropertyOrder(0)]
        public required string UserId { get; set; }

        [JsonPropertyName("channelId")]
        [JsonPropertyOrder(1)]
        public required ChannelId ChannelId { get; set; }

        [JsonPropertyName("includeFilter")]
        [JsonPropertyOrder(2)]
        public string? IncludeFilter { get; set; }
    }

    public class SignOutRequest
    {
        [JsonPropertyName("userId")]
        [JsonPropertyOrder(0)]
        public required string UserId { get; set; }

        [JsonPropertyName("connectionName")]
        [JsonPropertyOrder(1)]
        public required string ConnectionName { get; set; }

        [JsonPropertyName("channelId")]
        [JsonPropertyOrder(2)]
        public required ChannelId ChannelId { get; set; }
    }

    public class ExchangeTokenRequest
    {
        [JsonPropertyName("userId")]
        [JsonPropertyOrder(0)]
        public required string UserId { get; set; }

        [JsonPropertyName("connectionName")]
        [JsonPropertyOrder(1)]
        public required string ConnectionName { get; set; }

        [JsonPropertyName("channelId")]
        [JsonPropertyOrder(2)]
        public required ChannelId ChannelId { get; set; }

        [JsonPropertyName("exchangeRequest")]
        [JsonPropertyOrder(3)]
        public required TokenExchange.Request ExchangeRequest { get; set; }

        internal TokenExchange.Request GetBody() => ExchangeRequest;
    }
}