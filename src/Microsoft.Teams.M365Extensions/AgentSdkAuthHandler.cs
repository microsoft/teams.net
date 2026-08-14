// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.Agents.Authentication;

namespace Microsoft.Teams.M365Extensions;

/// <summary>
/// A <see cref="DelegatingHandler"/> that bridges the Microsoft 365 Agents SDK's authentication
/// (<see cref="IConnections"/> / <see cref="IAccessTokenProvider"/>) into the
/// Teams SDK's outbound HTTP pipeline, so the Teams SDK's <c>ConversationClient</c>
/// sends authenticated requests without a separate AzureAd config section.
/// </summary>
/// <remarks>
/// This bridge acquires <b>app-only</b> tokens via the Agents SDK connection manager. It does not
/// yet forward agentic (user-delegated) identities: the Teams SDK stamps an agentic identity onto
/// the request options (carrying <c>AgenticUserId</c> as a GUID, the form the stock
/// <c>BotAuthenticationHandler</c> feeds to MSAL's <c>WithAgentUserIdentity</c>), whereas the
/// Agents SDK's <c>IAgenticTokenProvider.GetAgenticUserTokenAsync</c> expects a UPN. Bridging the
/// two requires a GUID→UPN resolution that is out of scope here, so agentic turns currently fall
/// back to app-only auth. Tracked as a follow-up.
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
                string token = await tokenProvider.GetAccessTokenAsync(serviceUrl, scopes).ConfigureAwait(false);

                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
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
