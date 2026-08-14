// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.Agents.Authentication;
using Microsoft.Teams.Core.Http;
using Microsoft.Teams.Core.Schema;

namespace Microsoft.Teams.M365Extensions;

/// <summary>
/// A <see cref="DelegatingHandler"/> that bridges the Microsoft 365 Agents SDK's authentication
/// (<see cref="IConnections"/> / <see cref="IAccessTokenProvider"/>) into the
/// Teams SDK's outbound HTTP pipeline, so the Teams SDK's <c>ConversationClient</c>
/// sends authenticated requests without a separate AzureAd config section.
/// </summary>
/// <remarks>
/// By default the bridge acquires an <b>app-only</b> token via the Agents SDK connection manager.
/// When the Teams SDK stamps an agentic identity onto the request options
/// (<see cref="BotRequestContext.AgenticIdentityKey"/>) and the selected connection provider
/// supports <see cref="IAgenticTokenProvider"/>, it instead acquires a <b>user-delegated (agentic)</b>
/// token via <see cref="IAgenticTokenProvider.GetAgenticUserTokenAsync"/> — mirroring the TypeScript
/// and Python packages. Requests fall back to app-only auth when no (complete) agentic identity is
/// present or the provider is not agentic-capable.
/// <para>
/// Note: the agentic token is requested with the connection's configured scopes
/// (<c>ConnectionSettings.Scopes</c>), matching the app-only path and the TS/PY behavior of passing
/// the requested scope through.
/// </para>
/// </remarks>
internal sealed class AgentSdkAuthHandler(
    IConnections connections) : DelegatingHandler
{
    private readonly IConnections _connections = connections;

    /// <inheritdoc/>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Without a target URI we can neither select a connection nor a resource,
        // so leave the request unauthenticated and let the inner handler send it.
        Uri? requestUri = request.RequestUri;
        if (requestUri is not null)
        {
            IAccessTokenProvider tokenProvider = GetTokenProvider(requestUri);

            if (tokenProvider != null)
            {
                string serviceUrl = requestUri.GetLeftPart(UriPartial.Authority);
                var scopes = tokenProvider.ConnectionSettings.Scopes;
                string token = await AcquireTokenAsync(tokenProvider, request, serviceUrl, scopes, cancellationToken).ConfigureAwait(false);

                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<string> AcquireTokenAsync(
        IAccessTokenProvider tokenProvider,
        HttpRequestMessage request,
        string serviceUrl,
        IList<string> scopes,
        CancellationToken cancellationToken)
    {
        // When the Teams SDK is acting on behalf of an agentic user identity and the connection
        // provider can mint agentic tokens, acquire a user-delegated token; otherwise app-only.
        if (tokenProvider is IAgenticTokenProvider agenticProvider
            && TryGetAgenticIdentity(request, out AgenticIdentity? agentic)
            && !string.IsNullOrEmpty(agentic.AgenticAppId)
            && !string.IsNullOrEmpty(agentic.AgenticUserId))
        {
            return await agenticProvider.GetAgenticUserTokenAsync(
                agentic.TenantId ?? string.Empty,
                agentic.AgenticAppId,
                agentic.AgenticUserId,
                scopes,
                cancellationToken).ConfigureAwait(false);
        }

        return await tokenProvider.GetAccessTokenAsync(serviceUrl, scopes).ConfigureAwait(false);
    }

    private static bool TryGetAgenticIdentity(HttpRequestMessage request, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out AgenticIdentity? agenticIdentity)
    {
        // The Teams SDK's BotHttpClient stamps the agentic identity onto the request options as an
        // object under BotRequestContext.AgenticIdentityKey (see BotHttpClient / BotRequestContext).
        if (request.Options.TryGetValue(new HttpRequestOptionsKey<object?>(BotRequestContext.AgenticIdentityKey), out object? raw)
            && raw is AgenticIdentity identity)
        {
            agenticIdentity = identity;
            return true;
        }

        agenticIdentity = null;
        return false;
    }

    private IAccessTokenProvider GetTokenProvider(Uri requestUri)
    {
        string serviceUrl = requestUri.GetLeftPart(UriPartial.Authority);
        ClaimsIdentity? claimsIdentity = TeamsSdkMiddleware.CurrentTurnContext?.Identity;

        if (claimsIdentity?.IsAuthenticated == true)
        {
            var provider = _connections.GetTokenProvider(claimsIdentity, serviceUrl);
            if (provider != null)
            {
                return provider;
            }
        }

        return _connections.GetDefaultConnection();
    }
}
