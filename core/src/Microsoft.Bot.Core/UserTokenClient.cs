// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Bot.Core;
using Microsoft.Bot.Core.Hosting;
using Microsoft.Bot.Core.Schema;
using System.Text;
using System.Text.Json;

namespace Microsoft.Bot.Core;

/// <summary>
/// Client for managing user tokens via HTTP requests.
/// </summary>
/// <param name="logger"></param>
/// <param name="httpClient"></param>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1848:Use the LoggerMessage delegates", Justification = "<Pending>")]
public class UserTokenClient(HttpClient httpClient, ILogger<UserTokenClient> logger)
{
    internal const string UserTokenHttpClientName = "BotUserTokenClient";
    private readonly ILogger<UserTokenClient> _logger = logger;
    private readonly string _apiEndpoint = "https://token.botframework.com";
    private readonly JsonSerializerOptions _defaultOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    internal AgenticIdentity? AgenticIdentity { get; set; }

    /// <summary>
    /// Gets the token status for each connection for the given user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="channelId">The channel ID.</param>
    /// <param name="include">The optional include parameter.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    public async Task<GetTokenStatusResult[]> GetTokenStatusAsync(string userId, string channelId, string? include = null, CancellationToken cancellationToken = default)
    {
        Dictionary<string, string?> queryParams = new()
        {
            { "userid", userId },
            { "channelId", channelId }
        };

        if (!string.IsNullOrEmpty(include))
        {
            queryParams.Add("include", include);
        }

        string? resJson = await CallApiAsync("api/usertoken/GetTokenStatus", queryParams, cancellationToken: cancellationToken).ConfigureAwait(false);
        IList<GetTokenStatusResult> result = JsonSerializer.Deserialize<IList<GetTokenStatusResult>>(resJson!, _defaultOptions)!;
        if (result == null || result.Count == 0)
        {
            return [new GetTokenStatusResult { HasToken = false }];
        }
        return [.. result];

    }

    /// <summary>
    /// Gets the user token for a particular connection.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="connectionName">The connection name.</param>
    /// <param name="channelId">The channel ID.</param>
    /// <param name="code">The optional code.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    public async Task<GetTokenResult> GetTokenAsync(string userId, string connectionName, string channelId, string? code = null, CancellationToken cancellationToken = default)
    {
        Dictionary<string, string?> queryParams = new()
        {
            { "userid", userId },
            { "connectionName", connectionName },
            { "channelId", channelId }
        };

        if (!string.IsNullOrEmpty(code))
        {
            queryParams.Add("code", code);
        }

        string? resJson = await CallApiAsync("api/usertoken/GetToken", queryParams, cancellationToken: cancellationToken).ConfigureAwait(false);
        GetTokenResult result = JsonSerializer.Deserialize<GetTokenResult>(resJson!, _defaultOptions)!;
        return result;
    }

    /// <summary>
    /// Get the token or raw signin link to be sent to the user for signin for a connection.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="connectionName">The connection name.</param>
    /// <param name="channelId">The channel ID.</param>
    /// <param name="finalRedirect">The optional final redirect URL.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    public async Task<GetSignInResourceResult> GetSignInResource(string userId, string connectionName, string channelId, string? finalRedirect = null, CancellationToken cancellationToken = default)
    {
        var tokenExchangeState = new
        {
            ConnectionName = connectionName,
            Conversation = new
            {
                User = new ConversationAccount { Id = userId },
            }
        };
        var tokenExchangeStateJson = JsonSerializer.Serialize(tokenExchangeState, _defaultOptions);
        var state = Convert.ToBase64String(Encoding.UTF8.GetBytes(tokenExchangeStateJson));

        Dictionary<string, string?> queryParams = new()
        {
            { "state", state }
        };

        if (!string.IsNullOrEmpty(finalRedirect))
        {
            queryParams.Add("finalRedirect", finalRedirect);
        }

        string? resJson = await CallApiAsync("api/botsignin/GetSignInResource", queryParams, cancellationToken: cancellationToken).ConfigureAwait(false);
        GetSignInResourceResult result = JsonSerializer.Deserialize<GetSignInResourceResult>(resJson!, _defaultOptions)!;
        return result;
    }

    /// <summary>
    /// Exchanges a token for another token.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="connectionName">The connection name.</param>
    /// <param name="channelId">The channel ID.</param>
    /// <param name="exchangeToken">The token to exchange.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<GetTokenResult> ExchangeTokenAsync(string userId, string connectionName, string channelId, string? exchangeToken, CancellationToken cancellationToken = default)
    {
        Dictionary<string, string?> queryParams = new()
        {
            { "userid", userId },
            { "connectionName", connectionName },
            { "channelId", channelId }
        };

        var tokenExchangeRequest = new
        {
            token = exchangeToken
        };

        string? resJson = await CallApiAsync("api/usertoken/exchange", queryParams, method: HttpMethod.Post, JsonSerializer.Serialize(tokenExchangeRequest), cancellationToken).ConfigureAwait(false);
        GetTokenResult result =  JsonSerializer.Deserialize<GetTokenResult>(resJson!, _defaultOptions)!;
        return result!;
    }

    /// <summary>
    /// Signs the user out of a connection.
    /// <param name="userId">The user ID.</param>
    /// <param name="connectionName">The connection name.</param>
    /// <param name="channelId">The channel ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// </summary>
    public async Task SignOutUserAsync(string userId, string? connectionName = null, string? channelId = null, CancellationToken cancellationToken = default)
    {
        Dictionary<string, string?> queryParams = new()
        {
            { "userid", userId }
        };

        if (!string.IsNullOrEmpty(connectionName))
        {
            queryParams.Add("connectionName", connectionName);
        }

        if (!string.IsNullOrEmpty(channelId))
        {
            queryParams.Add("channelId", channelId);
        }

            await CallApiAsync("api/usertoken/SignOut", queryParams, HttpMethod.Delete, cancellationToken: cancellationToken).ConfigureAwait(false);
            return;
        }

    /// <summary>
    /// Gets AAD tokens for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="connectionName">The connection name.</param>
    /// <param name="channelId">The channel ID.</param>
    /// <param name="resourceUrls">The resource URLs.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    public async Task<IDictionary<string, GetTokenResult>> GetAadTokensAsync(string userId, string connectionName, string channelId, string[]? resourceUrls = null, CancellationToken cancellationToken = default)
    {
        var body = new
        {
            channelId,
            connectionName,
            userId,
            resourceUrls = resourceUrls ?? []
        };

        string? respJson = await CallApiAsync("api/usertoken/GetAadTokens", body, cancellationToken).ConfigureAwait(false);
        IDictionary<string, GetTokenResult> res = JsonSerializer.Deserialize<Dictionary<string, GetTokenResult>>(respJson!, _defaultOptions)!;
        return res;
    }

    private async Task<string?> CallApiAsync(string endpoint, Dictionary<string, string?> queryParams, HttpMethod? method = null, string? body = null, CancellationToken cancellationToken = default)
    {

        var fullPath = $"{_apiEndpoint}/{endpoint}";
        var requestUri = QueryHelpers.AddQueryString(fullPath, queryParams);
        _logger.LogInformation("Calling API endpoint: {Endpoint}", requestUri);

        HttpMethod httpMethod = method ?? HttpMethod.Get;
        #pragma warning disable CA2000 // HttpClient.SendAsync disposes the request
        HttpRequestMessage request = new(httpMethod, requestUri);
        #pragma warning restore CA2000

        // Pass the agentic identity to the handler via request options
        request.Options.Set(BotAuthenticationHandler.AgenticIdentityKey, AgenticIdentity);

        if (httpMethod == HttpMethod.Post && !string.IsNullOrEmpty(body))
        {
            request.Content = new StringContent(body, Encoding.UTF8, "application/json");
        }

        HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("API call successful. Status: {StatusCode}", response.StatusCode);
            return content;
        }
        else
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("User Token not found: {Endpoint}", requestUri);
                return null;
            }
            else
            {
                _logger.LogError("API call failed. Status: {StatusCode}, Error: {Error}",
                    response.StatusCode, errorContent);
                throw new HttpRequestException($"API call failed with status {response.StatusCode}: {errorContent}");
            }
        }
    }

    private async Task<string> CallApiAsync(string endpoint, object body, CancellationToken cancellationToken = default)
    {
        var fullPath = $"{_apiEndpoint}/{endpoint}";

        _logger.LogInformation("Calling API endpoint with POST: {Endpoint}", fullPath);

        var jsonContent = JsonSerializer.Serialize(body);
        StringContent content = new(jsonContent, Encoding.UTF8, "application/json");

        #pragma warning disable CA2000 // HttpClient.SendAsync disposes the request
        HttpRequestMessage request = new(HttpMethod.Post, fullPath)
        {
            Content = content
        };
        #pragma warning restore CA2000

        request.Options.Set(BotAuthenticationHandler.AgenticIdentityKey, AgenticIdentity);

        HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

        if (response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("API call successful. Status: {StatusCode}", response.StatusCode);
            return responseContent;
        }
        else
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogError("API call failed. Status: {StatusCode}, Error: {Error}",
                response.StatusCode, errorContent);
            throw new HttpRequestException($"API call failed with status {response.StatusCode}: {errorContent}");
        }
    }
}
