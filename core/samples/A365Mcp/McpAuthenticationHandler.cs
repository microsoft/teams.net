// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net.Http.Headers;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using Microsoft.Teams.Core.Schema;

namespace A365Mcp;

/// <summary>
/// HTTP message handler that acquires and attaches authentication tokens
/// for MCP server calls using agentic (user-delegated) token acquisition.
/// Each instance is bound to a specific <see cref="AgenticIdentity"/>.
/// </summary>
internal sealed class McpAuthenticationHandler(
    IAuthorizationHeaderProvider authorizationHeaderProvider,
    AgenticIdentity agenticIdentity,
    ILogger<McpAuthenticationHandler> logger,
    string authenticationOptionsName = "AzureAd") : DelegatingHandler(new HttpClientHandler())
{
    private const string McpScope = "ea9ffc3e-8a23-4a7d-836d-234d7c7565c1/.default";

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        string token = await GetAuthorizationHeaderAsync(cancellationToken).ConfigureAwait(false);

        string tokenValue = token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? token["Bearer ".Length..]
            : token;

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenValue);

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }

    private async Task<string> GetAuthorizationHeaderAsync(CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(agenticIdentity.AgenticAppId);
        ArgumentNullException.ThrowIfNullOrEmpty(agenticIdentity.AgenticUserId);

        if (!Guid.TryParse(agenticIdentity.AgenticUserId, out Guid agenticUserGuid))
        {
            throw new InvalidOperationException($"Invalid AgenticUserId '{agenticIdentity.AgenticUserId}'.");
        }

        logger.LogDebug("Acquiring agentic MCP token for AgenticAppId {AgenticAppId}", agenticIdentity.AgenticAppId);

        var options = new AuthorizationHeaderProviderOptions()
        {
            AcquireTokenOptions = new()
            {
                AuthenticationOptionsName = authenticationOptionsName,
            }
        }.WithAgentUserIdentity(agenticIdentity.AgenticAppId, agenticUserGuid);

        return await authorizationHeaderProvider.CreateAuthorizationHeaderAsync(
            [McpScope], options, cancellationToken: cancellationToken).ConfigureAwait(false);
    }
}
