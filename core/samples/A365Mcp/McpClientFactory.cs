// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Identity.Abstractions;
using Microsoft.Teams.Core.Schema;
using ModelContextProtocol.Client;

namespace A365Mcp;

/// <summary>
/// Creates authenticated <see cref="McpClient"/> instances using a
/// <see cref="McpAuthenticationHandler"/> that transparently attaches
/// user-delegated tokens to outbound MCP HTTP requests.
/// </summary>
internal sealed class McpClientFactory(
    IAuthorizationHeaderProvider authorizationHeaderProvider,
    ILogger<McpAuthenticationHandler> handlerLogger,
    IConfiguration configuration) : IMcpClientFactory
{
    private const string DefaultMcpServerUrl = "https://agent365.svc.cloud.microsoft/agents/servers/mcp_TeamsServer";

    public async Task<McpClient> CreateClientAsync(AgenticIdentity agenticIdentity, CancellationToken cancellationToken = default)
    {
        string mcpServerUrl = configuration["Mcp:ServerUrl"] ?? DefaultMcpServerUrl;

        var handler = new McpAuthenticationHandler(
            authorizationHeaderProvider,
            agenticIdentity,
            handlerLogger);

        var httpClient = new HttpClient(handler);

        return await McpClient.CreateAsync(
            new HttpClientTransport(new()
            {
                Endpoint = new Uri(mcpServerUrl),
                Name = "Agent365 Teams Client"
            }, httpClient), cancellationToken: cancellationToken).ConfigureAwait(false);
    }
}
