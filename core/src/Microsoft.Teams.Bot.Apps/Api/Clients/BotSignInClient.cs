// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Bot.Core;
using Microsoft.Teams.Bot.Core.Http;

namespace Microsoft.Teams.Bot.Apps.Api.Clients;

/// <summary>
/// Client for bot sign-in operations.
/// </summary>
public class BotSignInClient
{
    private readonly BotHttpClient _http;
    private readonly string _tokenApiEndpoint;

    internal BotSignInClient(BotHttpClient http, string tokenApiEndpoint = "https://token.botframework.com")
    {
        _http = http;
        _tokenApiEndpoint = tokenApiEndpoint.TrimEnd('/');
    }

    /// <summary>
    /// Get the sign-in URL for a connection.
    /// </summary>
    public async Task<string?> GetUrlAsync(string state, string? codeChallenge = null, Uri? emulatorUrl = null, Uri? finalRedirect = null, CancellationToken cancellationToken = default)
    {
        List<string> queryParams = [$"state={Uri.EscapeDataString(state)}"];

        if (!string.IsNullOrEmpty(codeChallenge))
            queryParams.Add($"code_challenge={Uri.EscapeDataString(codeChallenge)}");
        if (emulatorUrl is not null)
            queryParams.Add($"emulatorUrl={Uri.EscapeDataString(emulatorUrl.ToString())}");
        if (finalRedirect is not null)
            queryParams.Add($"finalRedirect={Uri.EscapeDataString(finalRedirect.ToString())}");

        string url = $"{_tokenApiEndpoint}/api/botsignin/GetSignInUrl?{string.Join("&", queryParams)}";
        return await _http.SendAsync<string>(HttpMethod.Get, url, body: null, options: null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Get the sign-in resource for a connection.
    /// </summary>
    public async Task<GetSignInResourceResult?> GetResourceAsync(string state, string? codeChallenge = null, Uri? emulatorUrl = null, Uri? finalRedirect = null, CancellationToken cancellationToken = default)
    {
        List<string> queryParams = [$"state={Uri.EscapeDataString(state)}"];

        if (!string.IsNullOrEmpty(codeChallenge))
            queryParams.Add($"code_challenge={Uri.EscapeDataString(codeChallenge)}");
        if (emulatorUrl is not null)
            queryParams.Add($"emulatorUrl={Uri.EscapeDataString(emulatorUrl.ToString())}");
        if (finalRedirect is not null)
            queryParams.Add($"finalRedirect={Uri.EscapeDataString(finalRedirect.ToString())}");

        string url = $"{_tokenApiEndpoint}/api/botsignin/GetSignInResource?{string.Join("&", queryParams)}";
        return await _http.SendAsync<GetSignInResourceResult>(HttpMethod.Get, url, body: null, options: null, cancellationToken).ConfigureAwait(false);
    }
}
