// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Teams.Bot.Core;
using Microsoft.Teams.Bot.Core.Http;

namespace Microsoft.Teams.Bot.Apps.Api.Clients;

/// <summary>
/// Client for user token operations.
/// </summary>
public class V3UserTokenClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly BotHttpClient _http;
    private readonly string _tokenApiEndpoint;

    internal V3UserTokenClient(BotHttpClient http, string tokenApiEndpoint = "https://token.botframework.com")
    {
        _http = http;
        _tokenApiEndpoint = tokenApiEndpoint.TrimEnd('/');
    }

    /// <summary>
    /// Get a user token for a connection.
    /// </summary>
    public async Task<GetTokenResult?> GetAsync(string userId, string connectionName, string channelId, string? code = null, CancellationToken cancellationToken = default)
    {
        List<string> queryParams =
        [
            $"userId={Uri.EscapeDataString(userId)}",
            $"connectionName={Uri.EscapeDataString(connectionName)}",
            $"channelId={Uri.EscapeDataString(channelId)}"
        ];

        if (!string.IsNullOrEmpty(code))
            queryParams.Add($"code={Uri.EscapeDataString(code)}");

        string url = $"{_tokenApiEndpoint}/api/usertoken/GetToken?{string.Join("&", queryParams)}";

        return await _http.SendAsync<GetTokenResult>(
            HttpMethod.Get, url, body: null,
            new BotRequestOptions { ReturnNullOnNotFound = true },
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Get AAD tokens for specified resources.
    /// </summary>
    public async Task<IDictionary<string, GetTokenResult>?> GetAadAsync(string userId, string connectionName, string channelId, IList<string>? resourceUrls = null, CancellationToken cancellationToken = default)
    {
        List<string> queryParams =
        [
            $"userId={Uri.EscapeDataString(userId)}",
            $"connectionName={Uri.EscapeDataString(connectionName)}",
            $"channelId={Uri.EscapeDataString(channelId)}"
        ];

        string url = $"{_tokenApiEndpoint}/api/usertoken/GetAadTokens?{string.Join("&", queryParams)}";
        var body = new { resourceUrls = resourceUrls ?? new List<string>() };
        string bodyJson = JsonSerializer.Serialize(body, JsonOptions);

        return await _http.SendAsync<IDictionary<string, GetTokenResult>>(
            HttpMethod.Post, url, bodyJson, null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Get the token status for a user's connections.
    /// </summary>
    public async Task<IList<GetTokenStatusResult>?> GetStatusAsync(string userId, string channelId, string? includeFilter = null, CancellationToken cancellationToken = default)
    {
        List<string> queryParams =
        [
            $"userId={Uri.EscapeDataString(userId)}",
            $"channelId={Uri.EscapeDataString(channelId)}"
        ];

        if (!string.IsNullOrEmpty(includeFilter))
            queryParams.Add($"includeFilter={Uri.EscapeDataString(includeFilter)}");

        string url = $"{_tokenApiEndpoint}/api/usertoken/GetTokenStatus?{string.Join("&", queryParams)}";
        return await _http.SendAsync<IList<GetTokenStatusResult>>(
            HttpMethod.Get, url, body: null, options: null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sign a user out of a connection.
    /// </summary>
    public async Task SignOutAsync(string userId, string connectionName, string channelId, CancellationToken cancellationToken = default)
    {
        List<string> queryParams =
        [
            $"userId={Uri.EscapeDataString(userId)}",
            $"connectionName={Uri.EscapeDataString(connectionName)}",
            $"channelId={Uri.EscapeDataString(channelId)}"
        ];

        string url = $"{_tokenApiEndpoint}/api/usertoken/SignOut?{string.Join("&", queryParams)}";
        await _http.SendAsync(HttpMethod.Delete, url, body: null, options: null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Exchange a token for another token.
    /// </summary>
    public async Task<GetTokenResult?> ExchangeAsync(string userId, string connectionName, string channelId, string exchangeToken, CancellationToken cancellationToken = default)
    {
        List<string> queryParams =
        [
            $"userId={Uri.EscapeDataString(userId)}",
            $"connectionName={Uri.EscapeDataString(connectionName)}",
            $"channelId={Uri.EscapeDataString(channelId)}"
        ];

        string url = $"{_tokenApiEndpoint}/api/usertoken/exchange?{string.Join("&", queryParams)}";
        var body = new { token = exchangeToken };
        string bodyJson = JsonSerializer.Serialize(body, JsonOptions);

        return await _http.SendAsync<GetTokenResult>(
            HttpMethod.Post, url, bodyJson, null, cancellationToken).ConfigureAwait(false);
    }
}
