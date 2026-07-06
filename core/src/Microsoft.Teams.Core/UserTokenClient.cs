// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Teams.Core.Http;
using Microsoft.Teams.Core.Schema;

namespace Microsoft.Teams.Core;

/// <summary>
/// Client for managing user tokens via HTTP requests to the Bot Framework Token Service.
/// </summary>
/// <remarks>
/// This client provides methods for OAuth token management including retrieving tokens, exchanging tokens,
/// signing out users, and managing AAD tokens. The client communicates with the Bot Framework Token Service
/// API endpoint (defaults to https://token.botframework.com but can be configured via UserTokenApiEndpoint).
/// </remarks>
/// <param name="httpClient">The HTTP client for making requests to the token service.</param>
/// <param name="configuration">Configuration containing the UserTokenApiEndpoint setting and other bot configuration.</param>
/// <param name="logger">Logger for diagnostic information and request tracking.</param>
public class UserTokenClient(HttpClient httpClient, IConfiguration configuration, ILogger<UserTokenClient> logger)
{
    internal const string UserTokenHttpClientName = "BotUserTokenClient";
    private readonly BotHttpClient _botHttpClient = new(httpClient, logger);
    private readonly string _apiEndpoint = configuration["UserTokenApiEndpoint"] ?? "https://token.botframework.com";
    private readonly JsonSerializerOptions _defaultOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    /// <summary>
    /// Gets the token status for each connection for the given user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="channelId">The channel ID.</param>
    /// <param name="include">The optional include parameter.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The result contains an array of token status results for each connection.</returns>
    public virtual async Task<GetTokenStatusResult[]> GetTokenStatusAsync(string userId, string channelId, string? include = null, CancellationToken cancellationToken = default)
        => await GetTokenStatusAsync(userId, channelId, include, requestContext: null, cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Gets the token status for each connection for the given user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="channelId">The channel ID.</param>
    /// <param name="include">The optional include parameter.</param>
    /// <param name="requestContext">Optional per-request properties used for authentication.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The result contains an array of token status results for each connection.</returns>
    public virtual async Task<GetTokenStatusResult[]> GetTokenStatusAsync(string userId, string channelId, string? include, BotRequestContext? requestContext, CancellationToken cancellationToken = default)
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
        IList<GetTokenStatusResult>? result = await _botHttpClient.SendAsync<IList<GetTokenStatusResult>>(
            HttpMethod.Get,
            _apiEndpoint,
            "api/usertoken/GetTokenStatus",
            queryParams,
            body: null,
            CreateRequestOptions("getting token status", requestContext: requestContext),
            cancellationToken).ConfigureAwait(false);

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
    /// <returns>A task that represents the asynchronous operation. The result contains the token, or null if no token is available.</returns>
    public virtual async Task<GetTokenResult?> GetTokenAsync(string userId, string connectionName, string channelId, string? code = null, CancellationToken cancellationToken = default)
        => await GetTokenAsync(userId, connectionName, channelId, code, requestContext: null, cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Gets the user token for a particular connection.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="connectionName">The connection name.</param>
    /// <param name="channelId">The channel ID.</param>
    /// <param name="code">The optional code.</param>
    /// <param name="requestContext">Optional per-request properties used for authentication.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The result contains the token, or null if no token is available.</returns>
    public virtual async Task<GetTokenResult?> GetTokenAsync(string userId, string connectionName, string channelId, string? code, BotRequestContext? requestContext, CancellationToken cancellationToken = default)
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

        return await _botHttpClient.SendAsync<GetTokenResult>(
            HttpMethod.Get,
            _apiEndpoint,
            "api/usertoken/GetToken",
            queryParams,
            body: null,
            CreateRequestOptions("getting token", returnNullOnNotFound: true, requestContext: requestContext),
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Get the token or raw signin link to be sent to the user for signin for a connection.
    /// Builds the state parameter internally from the userId and connectionName.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="connectionName">The connection name.</param>
    /// <param name="channelId">The channel ID.</param>
    /// <param name="finalRedirect">The optional final redirect URL.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The result contains the sign-in resource with the sign-in link and token exchange information.</returns>
    public virtual Task<GetSignInResourceResult> GetSignInResourceAsync(string userId, string connectionName, string channelId, string? finalRedirect = null, CancellationToken cancellationToken = default)
        => GetSignInResourceAsync(userId, connectionName, channelId, finalRedirect, requestContext: null, cancellationToken);

    /// <summary>
    /// Get the token or raw signin link to be sent to the user for signin for a connection.
    /// Builds the state parameter internally from the userId and connectionName.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="connectionName">The connection name.</param>
    /// <param name="channelId">The channel ID.</param>
    /// <param name="finalRedirect">The optional final redirect URL.</param>
    /// <param name="requestContext">Optional per-request properties used for authentication.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The result contains the sign-in resource with the sign-in link and token exchange information.</returns>
    public virtual Task<GetSignInResourceResult> GetSignInResourceAsync(string userId, string connectionName, string channelId, string? finalRedirect, BotRequestContext? requestContext, CancellationToken cancellationToken = default)
    {
        var tokenExchangeState = new
        {
            ConnectionName = connectionName,
            Conversation = new
            {
                User = new ChannelAccount { Id = userId },
            }
        };
        string tokenExchangeStateJson = JsonSerializer.Serialize(tokenExchangeState, _defaultOptions);
        string state = Convert.ToBase64String(Encoding.UTF8.GetBytes(tokenExchangeStateJson));

        Uri? finalRedirectUri = finalRedirect is not null ? new Uri(finalRedirect) : null;
        return GetSignInResourceAsync(state, codeChallenge: null, emulatorUrl: null, finalRedirect: finalRedirectUri, requestContext: requestContext, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Gets the sign-in URL for the given state.
    /// </summary>
    /// <param name="state">The encoded state parameter.</param>
    /// <param name="codeChallenge">The optional code challenge for PKCE.</param>
    /// <param name="emulatorUrl">The optional emulator URL.</param>
    /// <param name="finalRedirect">The optional final redirect URL.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The sign-in URL, or null if not available.</returns>
    public virtual async Task<string?> GetSignInUrlAsync(string state, string? codeChallenge = null, Uri? emulatorUrl = null, Uri? finalRedirect = null, CancellationToken cancellationToken = default)
        => await GetSignInUrlAsync(state, codeChallenge, emulatorUrl, finalRedirect, requestContext: null, cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Gets the sign-in URL for the given state.
    /// </summary>
    /// <param name="state">The encoded state parameter.</param>
    /// <param name="codeChallenge">The optional code challenge for PKCE.</param>
    /// <param name="emulatorUrl">The optional emulator URL.</param>
    /// <param name="finalRedirect">The optional final redirect URL.</param>
    /// <param name="requestContext">Optional per-request properties used for authentication.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The sign-in URL, or null if not available.</returns>
    public virtual async Task<string?> GetSignInUrlAsync(string state, string? codeChallenge, Uri? emulatorUrl, Uri? finalRedirect, BotRequestContext? requestContext, CancellationToken cancellationToken = default)
    {
        Dictionary<string, string?> queryParams = new() { { "state", state } };

        if (!string.IsNullOrEmpty(codeChallenge))
            queryParams.Add("code_challenge", codeChallenge);
        if (emulatorUrl is not null)
            queryParams.Add("emulatorUrl", emulatorUrl.ToString());
        if (finalRedirect is not null)
            queryParams.Add("finalRedirect", finalRedirect.ToString());

        return await _botHttpClient.SendAsync<string>(
            HttpMethod.Get,
            _apiEndpoint,
            "api/botsignin/GetSignInUrl",
            queryParams,
            body: null,
            CreateRequestOptions("getting sign-in URL", requestContext: requestContext),
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the sign-in resource for the given state.
    /// </summary>
    /// <param name="state">The encoded state parameter.</param>
    /// <param name="codeChallenge">The optional code challenge for PKCE.</param>
    /// <param name="emulatorUrl">The optional emulator URL.</param>
    /// <param name="finalRedirect">The optional final redirect URL.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The sign-in resource result.</returns>
    public virtual async Task<GetSignInResourceResult> GetSignInResourceAsync(string state, string? codeChallenge = null, Uri? emulatorUrl = null, Uri? finalRedirect = null, CancellationToken cancellationToken = default)
        => await GetSignInResourceAsync(state, codeChallenge, emulatorUrl, finalRedirect, requestContext: null, cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Gets the sign-in resource for the given state.
    /// </summary>
    /// <param name="state">The encoded state parameter.</param>
    /// <param name="codeChallenge">The optional code challenge for PKCE.</param>
    /// <param name="emulatorUrl">The optional emulator URL.</param>
    /// <param name="finalRedirect">The optional final redirect URL.</param>
    /// <param name="requestContext">Optional per-request properties used for authentication.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The sign-in resource result.</returns>
    public virtual async Task<GetSignInResourceResult> GetSignInResourceAsync(string state, string? codeChallenge, Uri? emulatorUrl, Uri? finalRedirect, BotRequestContext? requestContext, CancellationToken cancellationToken = default)
    {
        Dictionary<string, string?> queryParams = new() { { "state", state } };

        if (!string.IsNullOrEmpty(codeChallenge))
            queryParams.Add("code_challenge", codeChallenge);
        if (emulatorUrl is not null)
            queryParams.Add("emulatorUrl", emulatorUrl.ToString());
        if (finalRedirect is not null)
            queryParams.Add("finalRedirect", finalRedirect.ToString());

        return (await _botHttpClient.SendAsync<GetSignInResourceResult>(
            HttpMethod.Get,
            _apiEndpoint,
            "api/botsignin/GetSignInResource",
            queryParams,
            body: null,
            CreateRequestOptions("getting sign-in resource", requestContext: requestContext),
            cancellationToken).ConfigureAwait(false))!;
    }

    /// <summary>
    /// Exchanges a token for another token.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="connectionName">The connection name.</param>
    /// <param name="channelId">The channel ID.</param>
    /// <param name="exchangeToken">The token to exchange.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public virtual async Task<GetTokenResult> ExchangeTokenAsync(string userId, string connectionName, string channelId, string? exchangeToken, CancellationToken cancellationToken = default)
        => await ExchangeTokenAsync(userId, connectionName, channelId, exchangeToken, requestContext: null, cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Exchanges a token for another token.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="connectionName">The connection name.</param>
    /// <param name="channelId">The channel ID.</param>
    /// <param name="exchangeToken">The token to exchange.</param>
    /// <param name="requestContext">Optional per-request properties used for authentication.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public virtual async Task<GetTokenResult> ExchangeTokenAsync(string userId, string connectionName, string channelId, string? exchangeToken, BotRequestContext? requestContext, CancellationToken cancellationToken = default)
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

        return (await _botHttpClient.SendAsync<GetTokenResult>(
            HttpMethod.Post,
            _apiEndpoint,
            "api/usertoken/exchange",
            queryParams,
            JsonSerializer.Serialize(tokenExchangeRequest),
            CreateRequestOptions("exchanging token", requestContext: requestContext),
            cancellationToken).ConfigureAwait(false))!;
    }

    /// <summary>
    /// Signs the user out of a connection, revoking their OAuth token.
    /// </summary>
    /// <param name="userId">The unique identifier of the user to sign out. Cannot be null or empty.</param>
    /// <param name="connectionName">Optional name of the OAuth connection to sign out from. If null, signs out from all connections.</param>
    /// <param name="channelId">Optional channel identifier. If provided, limits sign-out to tokens for this channel.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous sign-out operation.</returns>
    public virtual async Task SignOutUserAsync(string userId, string? connectionName = null, string? channelId = null, CancellationToken cancellationToken = default)
        => await SignOutUserAsync(userId, connectionName, channelId, requestContext: null, cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Signs the user out of a connection, revoking their OAuth token.
    /// </summary>
    /// <param name="userId">The unique identifier of the user to sign out. Cannot be null or empty.</param>
    /// <param name="connectionName">Optional name of the OAuth connection to sign out from. If null, signs out from all connections.</param>
    /// <param name="channelId">Optional channel identifier. If provided, limits sign-out to tokens for this channel.</param>
    /// <param name="requestContext">Optional per-request properties used for authentication.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous sign-out operation.</returns>
    public virtual async Task SignOutUserAsync(string userId, string? connectionName, string? channelId, BotRequestContext? requestContext, CancellationToken cancellationToken = default)
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

        await _botHttpClient.SendAsync(
            HttpMethod.Delete,
            _apiEndpoint,
            "api/usertoken/SignOut",
            queryParams,
            body: null,
            CreateRequestOptions("signing out user", requestContext: requestContext),
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets AAD tokens for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="connectionName">The connection name.</param>
    /// <param name="channelId">The channel ID.</param>
    /// <param name="resourceUrls">The resource URLs.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The result contains a dictionary mapping resource URLs to their token results.</returns>
    public virtual async Task<IDictionary<string, GetTokenResult>> GetAadTokensAsync(string userId, string connectionName, string channelId, string[]? resourceUrls = null, CancellationToken cancellationToken = default)
        => await GetAadTokensAsync(userId, connectionName, channelId, resourceUrls, requestContext: null, cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Gets AAD tokens for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="connectionName">The connection name.</param>
    /// <param name="channelId">The channel ID.</param>
    /// <param name="resourceUrls">The resource URLs.</param>
    /// <param name="requestContext">Optional per-request properties used for authentication.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The result contains a dictionary mapping resource URLs to their token results.</returns>
    public virtual async Task<IDictionary<string, GetTokenResult>> GetAadTokensAsync(string userId, string connectionName, string channelId, string[]? resourceUrls, BotRequestContext? requestContext, CancellationToken cancellationToken = default)
    {
        var body = new
        {
            channelId,
            connectionName,
            userId,
            resourceUrls = resourceUrls ?? []
        };

        return (await _botHttpClient.SendAsync<Dictionary<string, GetTokenResult>>(
            HttpMethod.Post,
            _apiEndpoint,
            "api/usertoken/GetAadTokens",
            queryParams: null,
            JsonSerializer.Serialize(body),
            CreateRequestOptions("getting AAD tokens", requestContext: requestContext),
            cancellationToken).ConfigureAwait(false))!;
    }

    private static BotRequestOptions CreateRequestOptions(string operationDescription, bool returnNullOnNotFound = false, BotRequestContext? requestContext = null) =>
        new()
        {
            RequestContext = requestContext,
            OperationDescription = operationDescription,
            ReturnNullOnNotFound = returnNullOnNotFound
        };
}
