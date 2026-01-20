// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text;
using System.Text.Json;
using Microsoft.Teams.Bot.Core.Http;
using Microsoft.Teams.Bot.Core.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using CoreAssemblyInfo;

namespace Microsoft.Teams.Bot.Core;

using CustomHeaders = Dictionary<string, string>;

/// <summary>
/// Client for managing user tokens via HTTP requests.
/// </summary>
/// <param name="configuration"></param>
/// <param name="logger"></param>
/// <param name="httpClient"></param>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1848:Use the LoggerMessage delegates", Justification = "<Pending>")]
public class UserTokenClient(HttpClient httpClient, IConfiguration configuration, ILogger<UserTokenClient> logger)
{
    internal const string UserTokenHttpClientName = "BotUserTokenClient";
    private readonly ILogger<UserTokenClient> _logger = logger;
    private readonly BotHttpClient _botHttpClient = new(httpClient, logger);
    private readonly string _apiEndpoint = configuration["UserTokenApiEndpoint"] ?? "https://token.botframework.com";
    private readonly JsonSerializerOptions _defaultOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    internal AgenticIdentity? AgenticIdentity { get; set; }

    /// <summary>
    /// Gets the default custom headers that will be included in all requests.
    /// </summary>
    public CustomHeaders DefaultCustomHeaders { get; } = new()
    {
        ["User-Agent"] = $"{ThisAssembly.AssemblyName}/{ThisAssembly.AssemblyInformationalVersion}"
    };

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

        _logger.LogInformation("Calling API endpoint: {Endpoint}", "api/usertoken/GetTokenStatus");
        IList<GetTokenStatusResult>? result = await _botHttpClient.SendAsync<IList<GetTokenStatusResult>>(
            HttpMethod.Get,
            _apiEndpoint,
            "api/usertoken/GetTokenStatus",
            queryParams,
            body: null,
            CreateRequestOptions("getting token status"),
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
    /// <returns></returns>
    public async Task<GetTokenResult?> GetTokenAsync(string userId, string connectionName, string channelId, string? code = null, CancellationToken cancellationToken = default)
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

        _logger.LogInformation("Calling API endpoint: {Endpoint}", "api/usertoken/GetToken");
        return await _botHttpClient.SendAsync<GetTokenResult>(
            HttpMethod.Get,
            _apiEndpoint,
            "api/usertoken/GetToken",
            queryParams,
            body: null,
            CreateRequestOptions("getting token", returnNullOnNotFound: true),
            cancellationToken).ConfigureAwait(false);
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
        string tokenExchangeStateJson = JsonSerializer.Serialize(tokenExchangeState, _defaultOptions);
        string state = Convert.ToBase64String(Encoding.UTF8.GetBytes(tokenExchangeStateJson));

        Dictionary<string, string?> queryParams = new()
        {
            { "state", state }
        };

        if (!string.IsNullOrEmpty(finalRedirect))
        {
            queryParams.Add("finalRedirect", finalRedirect);
        }

        _logger.LogInformation("Calling API endpoint: {Endpoint}", "api/botsignin/GetSignInResource");
        return (await _botHttpClient.SendAsync<GetSignInResourceResult>(
            HttpMethod.Get,
            _apiEndpoint,
            "api/botsignin/GetSignInResource",
            queryParams,
            body: null,
            CreateRequestOptions("getting sign-in resource"),
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

        _logger.LogInformation("Calling API endpoint: {Endpoint}", "api/usertoken/exchange");
        return (await _botHttpClient.SendAsync<GetTokenResult>(
            HttpMethod.Post,
            _apiEndpoint,
            "api/usertoken/exchange",
            queryParams,
            JsonSerializer.Serialize(tokenExchangeRequest),
            CreateRequestOptions("exchanging token"),
            cancellationToken).ConfigureAwait(false))!;
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

        _logger.LogInformation("Calling API endpoint: {Endpoint}", "api/usertoken/SignOut");
        await _botHttpClient.SendAsync(
            HttpMethod.Delete,
            _apiEndpoint,
            "api/usertoken/SignOut",
            queryParams,
            body: null,
            CreateRequestOptions("signing out user"),
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

        _logger.LogInformation("Calling API endpoint with POST: {Endpoint}", "api/usertoken/GetAadTokens");
        return (await _botHttpClient.SendAsync<Dictionary<string, GetTokenResult>>(
            HttpMethod.Post,
            _apiEndpoint,
            "api/usertoken/GetAadTokens",
            queryParams: null,
            JsonSerializer.Serialize(body),
            CreateRequestOptions("getting AAD tokens"),
            cancellationToken).ConfigureAwait(false))!;
    }

    private BotRequestOptions CreateRequestOptions(string operationDescription, bool returnNullOnNotFound = false) =>
        new()
        {
            AgenticIdentity = AgenticIdentity,
            OperationDescription = operationDescription,
            ReturnNullOnNotFound = returnNullOnNotFound,
            DefaultHeaders = DefaultCustomHeaders
        };
}
